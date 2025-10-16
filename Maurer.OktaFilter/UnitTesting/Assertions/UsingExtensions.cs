using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UnitTesting.Fixture;
using UnitTesting.Harness;

namespace UnitTesting.Assertions
{
    public class UsingExtensions : ExtensionsHarness
    {
        public UsingExtensions(ExtensionsFixture Fixture) : base(Fixture)
        {

        }

        [Fact]
        public void AddOktaFilter_ConfigBindsAndRegisters_AllServices()
        {
            var services = new ServiceCollection();
            services.AddLogging(); // TokenService uses ILogger

            services.AddOktaFilter(_fixture.HttpsConfiguration); // uses DistributedMemoryCache by default

            using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

            // Force options validation (you register OktaOptions as a singleton via IOptions.Value)
            var opts = serviceProvider.GetRequiredService<OktaOptions>();
            Assert.Equal("OKTA-TOKEN", opts.AUTHKEY);

            // Cache + helper + token service + filter resolve
            Assert.NotNull(serviceProvider.GetRequiredService<IDistributedCache>());
            Assert.NotNull(serviceProvider.GetRequiredService<IDistributedCacheHelper>());
            Assert.NotNull(serviceProvider.GetRequiredService<ITokenService>());

            using var scope = serviceProvider.CreateScope();
            Assert.NotNull(scope.ServiceProvider.GetRequiredService<AuthenticationFilter<TokenService>>());
        }

        [Fact]
        public void AddOktaFilter_InvalidHttpsUrl_ThrowsOptionsValidationException()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDistributedMemoryCache();

            services.AddOktaFilter(_fixture.HttpConfiguration);

            using var serviceProvider = services.BuildServiceProvider();

            // resolving OktaOptions forces validation to run
            Assert.Throws<OptionsValidationException>(() => serviceProvider.GetRequiredService<OktaOptions>());
        }

        [Fact]
        public void AddOktaFilter_CustomHttpClientPipeline_IsApplied()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOktaFilter(
                clientBuilder: http => http.ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(7)),
                configuration: _fixture.HttpsConfiguration);

            using var serviceProvider = services.BuildServiceProvider();
            // Just verify the service graph resolves; the timeout is part of the HttpClient created by the factory and used by TokenService.
            Assert.NotNull(serviceProvider.GetRequiredService<ITokenService>());
        }

        [Fact]
        public void AddOktaFilter_NoDistributedCacheRegistered_WillFail()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOktaFilter(_fixture.HttpsConfiguration, useDistributedMemoryCache: false);

            using var serviceProvider = services.BuildServiceProvider();
            Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<IDistributedCache>());
        }

        [Fact]
        public void AddOktaFilter_NoDistributedCacheInMethod_ButRegisteredByCaller_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDistributedMemoryCache(); // caller registers it

            services.AddOktaFilter(_fixture.HttpsConfiguration, useDistributedMemoryCache: false);

            using var serviceProvider = services.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetRequiredService<IDistributedCache>());
            Assert.NotNull(serviceProvider.GetRequiredService<IDistributedCacheHelper>());
        }

        [Fact]
        public void AddOktaFilter_CodeOnlyOptions_RegistersAndResolves()
        {
            var options = new OktaOptions
            {
                USER = "client_id",
                PASSWORD = "secret",
                OAUTHURL = "https://example.com/oauth2/v1/token",
                AUTHKEY = "OKTA-TOKEN",
                GRANT = "client_credentials",
                SCOPE = "openid",
                RETRIES = 1,
                SLEEP = 0,
                LIFETIME = 5
            };

            var services = new ServiceCollection();
            services.AddLogging();

            services.AddOktaFilter(options /* default: registers DistributedMemoryCache */);

            using var serviceProvider = services.BuildServiceProvider();
            Assert.Same(options, serviceProvider.GetRequiredService<OktaOptions>());
            Assert.NotNull(serviceProvider.GetRequiredService<IDistributedCache>());
            Assert.NotNull(serviceProvider.GetRequiredService<ITokenService>());
        }

        [Fact]
        public void AddOktaFilter_CodeOnlyOptions_BadUrl_ThrowsImmediately()
        {
            var options = new OktaOptions { OAUTHURL = "http://not-https" };
            var services = new ServiceCollection();
            Assert.Throws<ArgumentException>(() => services.AddOktaFilter(options));
        }
    }

}