using FluentAssertions;
using IntegrationTesting.Utilities;
using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IntegrationTesting
{
    public class IntegrationTests
    {
        public IntegrationTests()
        {
            HttpsConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Okta:USER"] = "client_id",
                ["Okta:PASSWORD"] = "secret",
                ["Okta:OAUTHURL"] = "https://secure.com/oauth2/v1/token",
                ["Okta:OAUTHKEY"] = "OKTA-TOKEN",
                ["Okta:GRANT"] = "client_credentials",
                ["Okta:SCOPE"] = "openid",
                ["Okta:RETRIES"] = "1",
                ["Okta:SLEEP"] = "0",
                ["Okta:LIFETIME"] = "5",
            }).Build();

            HttpConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Okta:USER"] = "client_id",
                ["Okta:PASSWORD"] = "secret",
                ["Okta:OAUTHURL"] = "http://insecure.local/token", // not HTTPS → should fail
                ["Okta:OAUTHKEY"] = "OKTA-TOKEN",
                ["Okta:GRANT"] = "client_credentials",
                ["Okta:SCOPE"] = "openid",
                ["Okta:RETRIES"] = "1",
                ["Okta:SLEEP"] = "0",
                ["Okta:LIFETIME"] = "5",
            }).Build();
        }

        public IConfiguration HttpsConfiguration { get; set; }

        public IConfiguration HttpConfiguration { get; set; }

        /// <summary>
        /// This test only resolves ITokenService in order to get a token, it doesn't touch the cache, the filter does that.
        /// </summary>

        [Fact]
        [Trait("Type", "Integration")]
        public async Task GetToken_Returns_ParsedToken_From_StubbedHttp()
        {
            var config = HttpsConfiguration;
            var services = new ServiceCollection();
            services.AddLogging();

            // Register the stub handler as singleton so we can read CallCount after calls
            var endpoint = new Uri(config["Okta:OAUTHURL"]!);
            var stub = new StubbedTokenHandler(endpoint,
                "{\"access_token\":\"aaa-first\",\"token_type\":\"Bearer\",\"expires_in\":300,\"scope\":\"openid\"}");

            // Use your overload that accepts HttpClient customization
            services.AddOktaFilter(
                clientBuilder: httpClientBuilder => httpClientBuilder.AddHttpMessageHandler(_ => stub),
                configuration: config);

            using var serviceProvider = services.BuildServiceProvider();
            var tokenService = serviceProvider.GetRequiredService<ITokenService>();

            var token = await tokenService.GetToken();
            token.Should().NotBeNull();
            token!.AccessToken.Should().Be("aaa-first");
            stub.CallCount.Should().Be(1);
        }

        /// <summary>
        /// First 'Get' filter has no entry, so it'll call 'GetToken' and write the result into the cache.  The second 'Get' will see the entry and skip the token call (stub.CallCount == 1). TestServer + MVC + filter + distributed cache + stubbed downstream HTTP,
        /// </summary>

        [Fact]
        [Trait("Type", "Integration")]
        public async Task First_Request_Caches_Token_Second_Request_Uses_Cache()
        {
            var configuration = HttpsConfiguration;

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();

            builder.Services
                   .AddControllers()
                   .AddApplicationPart(typeof(IntegrationController).Assembly) // <-- make MVC scan THIS assembly
                   .AddControllersAsServices();

            builder.Services.AddLogging();
            builder.Services.AddDistributedMemoryCache();

            // Stub Okta endpoint for TokenService
            var endpoint = new Uri(configuration["Okta:OAUTHURL"]!);
            var stub = new StubbedTokenHandler(endpoint,
                "{\"access_token\":\"first-token\",\"token_type\":\"Bearer\",\"expires_in\":300,\"scope\":\"openid\"}",
                "{\"access_token\":\"second-token\",\"token_type\":\"Bearer\",\"expires_in\":300,\"scope\":\"openid\"}");

            // Register your library with the stubbed HttpClient
            builder.Services.AddOktaFilter(
                clientBuilder: httpClientBuilder => httpClientBuilder.AddHttpMessageHandler(_ => stub),
                configuration: configuration);

            var app = builder.Build();
            app.MapControllers();

            await app.StartAsync();

            var client = app.GetTestClient();

            // Hit  new route
            var route1 = await client.GetAsync("/Integrations/AuthenticationFilter");
            route1.EnsureSuccessStatusCode();

            var route2 = await client.GetAsync("/Integrations/AuthenticationFilter");
            route2.EnsureSuccessStatusCode();

            // Token endpoint should have been called once; second call is cache hit
            stub.CallCount.Should().Be(1);

            //verify cache value
            var helper = app.Services.GetRequiredService<IDistributedCacheHelper>();
            var key = app.Services.GetRequiredService<OktaOptions>().OAUTHKEY;
            var tokenJson = await helper.Get(key);

            tokenJson.Should().NotBeNullOrWhiteSpace();
            tokenJson!.Should().Contain("first-token");
        }

        /// <summary>
        /// Proves extensions wire up the graph correctly, OtkaOptions bound and validated, cache registered, cache helper registered as singleton, typed HttpClient to ITokenService and scoped binding for AuthenticationFilter<TokenService>.
        /// </summary>

        [Fact]
        [Trait("Type", "Integration")]
        public void AddOktaFilter_ConfiguredGraph_Resolves_All_Primitives()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOktaFilter(HttpsConfiguration); // defaults to DistributedMemoryCache

            using var serviceProvider = services.BuildServiceProvider(
                new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

            // Singletons/transients are fine from root:
            serviceProvider.GetRequiredService<OktaOptions>().OAUTHKEY.Should().Be("OKTA-TOKEN");
            serviceProvider.GetRequiredService<IDistributedCache>().Should().NotBeNull();
            serviceProvider.GetRequiredService<IDistributedCacheHelper>().Should().NotBeNull();
            serviceProvider.GetRequiredService<ITokenService>().Should().NotBeNull();

            // Scoped service must be resolved from a scope:
            using var scope = serviceProvider.CreateScope();
            var scoped = scope.ServiceProvider;
            scoped.GetRequiredService<AuthenticationFilter<TokenService>>()
                  .Should().NotBeNull();
        }

        /// <summary>
        /// Verifies guardrails against HTTP calls
        /// </summary>

        [Fact]
        [Trait("Type", "Integration")]
        public void AddOktaFilter_BadHttpsUrl_Fails_On_OptionsResolve()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOktaFilter(HttpConfiguration);

            using var sp = services.BuildServiceProvider();
            FluentActions.Invoking(() => sp.GetRequiredService<OktaOptions>())
                .Should().Throw<OptionsValidationException>();
        }
    }
}
