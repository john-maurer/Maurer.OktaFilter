using Maurer.OktaFilter.Helpers;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Maurer.OktaFilter
{
    public static class Extensions
    {
        /// <summary>
        /// Core logic shared by all invariants.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="OKTASectionName">Name of the section for OKTA; defaults to "Okta".</param>
        /// <param name="configuration">Represents a set of "key/value" application configuration properties.</param>

        private static void AddOktaOptionsBound(this IServiceCollection services, IConfiguration configuration, string OKTASectionName = "Okta")
        {
            var oktaSection = configuration.GetSection(OKTASectionName);

            if (!oktaSection.Exists())
                throw new InvalidOperationException
                    ($"Configuration section '{OKTASectionName}' not found. Ensure configuration providers (appsettings, env vars, Azure Key Vault/App Configuration) are added before AddOktaFilter.");

            services.AddOptions<OktaOptions>()
                .Bind(configuration.GetSection(OKTASectionName))
                .ValidateDataAnnotations()
                .Validate(
                    options => Uri.TryCreate(options.OAUTHURL, UriKind.Absolute, out var uri) && 
                    uri.Scheme == Uri.UriSchemeHttps, "OAUTHURL must be an absolute HTTPS URL.")
                .ValidateOnStart();

            // TokenService currently consumes OktaOptions (not IOptions<OktaOptions>),
            // so expose the validated Value for DI.
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<OktaOptions>>().Value);
        }

        /// <summary>
        /// Set's the decompression methods of the primary message handler for the HttpClient to 'GZip' and 'Deflate'.  
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <returns>An instance IHttpClientBuilder with decompression methods set to 'GZip' and 'Deflate'.</returns>

        private static IHttpClientBuilder AddOktaHttpClient(this IServiceCollection services) =>
        
            services.AddHttpClient<ITokenService, TokenService>()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });

        /// <summary>
        /// Register closed generic filter so consumers can use [ServiceFilter(...)]
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>

        private static void AddOktaCommon(this IServiceCollection services)
        {
            services.TryAddSingleton<IDistributedCacheHelper, DistributedCacheHelper>();
            services.TryAddScoped<AuthenticationFilter<TokenService>>();
        }

        /// <summary>
        /// Bind options from IConfiguration, use DistributedMemoryCache by default, default HttpClient.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Specifies the contract for a collection of service descriptors.<./param>
        /// <param name="useDefaultCache">Determines if an in-memory alternative is used (true) or not (false).</param>
        /// <param name="OKTASectionName">Name of the section for OKTA; defaults to "Okta".</param>
        /// <param name="useDistributedMemoryCache">Determines whether or not a distributed cache is setup or an undistributed one is; true by default.</param>
        /// <returns>'AuthenticationFilter' configured as with an auth service client as 'Gzip' and 'Deflate'.</returns>

        public static IServiceCollection AddOktaFilter(this IServiceCollection services, IConfiguration configuration, bool useDefaultCache = true, string OKTASectionName = "Okta", bool useDistributedMemoryCache = true)
        {
            services.AddOktaOptionsBound(configuration, OKTASectionName);

            if (useDefaultCache)
            {
                if (useDistributedMemoryCache)
                    services.AddDistributedMemoryCache();
                else
                    services.AddMemoryCache();
            }

            services.AddOktaHttpClient();
            services.AddOktaCommon();

            return services;
        }

        /// <summary>
        /// Bind options from IConfiguration, use DistributedMemoryCache; facilitates customizable HttpClient.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="clientBuilder">Action encapsulating and HttpClientBuilder</param>
        /// <param name="useDefaultCache">Determines if an in-memory alternative is used (true) or not (false).</param>
        /// <param name="OKTASectionName">Name of the section for OKTA; defaults to "Okta".</param>
        /// <param name="useDistributedMemoryCache">Determines whether or not a distributed cache is setup or an undistributed one is; true by default.</param>
        /// <returns>'AuthenticationFilter' configured with a custom auth service client.</returns>

        public static IServiceCollection AddOktaFilter(this IServiceCollection services, IConfiguration configuration, Action<IHttpClientBuilder> clientBuilder, bool useDefaultCache = true, string OKTASectionName = "Okta", bool useDistributedMemoryCache = true)
        {
            services.AddOktaOptionsBound(configuration, OKTASectionName);

            if (useDefaultCache)
            {
                if (useDistributedMemoryCache)
                    services.AddDistributedMemoryCache();
                else
                    services.AddMemoryCache();
            }

            var http = services.AddOktaHttpClient();
            clientBuilder?.Invoke(http);
            services.AddOktaCommon();

            return services;
        }

        /// <summary>
        /// Code-only options, DistributedMemoryCache by default, default HttpClient.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="options">OktaOptions instance</param>
        /// <param name="useDefaultCache">Determines if an in-memory alternative is used (true) or not (false).</param>
        /// <param name="OKTASectionName">Name of the section for OKTA; defaults to "Okta".</param>
        /// <param name="useDistributedMemoryCache">>Determines whether or not a distributed cache is setup or an undistributed one is; true by default.</param>
        /// <returns>Collection of service descriptors modified for the AuthenticationFilter.</returns>
        /// <exception cref="ArgumentNullException">'OktaOptions' is null</exception>
        /// <exception cref="ArgumentException">OAUTHURL must be an absolute HTTPS URL.</exception>

        public static IServiceCollection AddOktaFilter(this IServiceCollection services, OktaOptions options, bool useDefaultCache = true, string OKTASectionName = "Okta", bool useDistributedMemoryCache = true)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (!Uri.TryCreate(options.OAUTHURL, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("OAUTHURL must be an absolute HTTPS URL.", nameof(options));

            services.TryAddSingleton(options);

            if (useDefaultCache)
            {
                if (useDistributedMemoryCache)
                    services.AddDistributedMemoryCache();
                else
                    services.AddMemoryCache();
            }

            services.AddOktaHttpClient();
            services.AddOktaCommon();
            return services;
        }

        /// <summary>
        /// Custom HttpClient (timeouts, handlers)
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="options">OktaOptions instance</param>
        /// <param name="configureHttp">An 'Action' on an 'IHttpClientBuilder'.</param>
        /// <param name="useDefaultCache">Determines if an in-memory alternative is used (true) or not (false).</param>
        /// <param name="useDistributedMemoryCache">>Determines whether or not a distributed cache is setup or an undistributed one is; true by default.</param>
        /// <returns>Collection of service descriptors modified for the AuthenticationFilter.</returns>
        /// <exception cref="ArgumentNullException">'OktaOptions' is null</exception>
        /// <exception cref="ArgumentException">OAUTHURL must be an absolute HTTPS URL.</exception>

        public static IServiceCollection AddOktaFilter(this IServiceCollection services, OktaOptions options, Action<IHttpClientBuilder> configureHttp, bool useDefaultCache = true, bool useDistributedMemoryCache = true)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (!Uri.TryCreate(options.OAUTHURL, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("OAUTHURL must be an absolute HTTPS URL.", nameof(options));

            services.TryAddSingleton(options);

            if (useDistributedMemoryCache)
                services.AddDistributedMemoryCache();
            else
                services.AddMemoryCache();

            configureHttp?.Invoke(services.AddOktaHttpClient());
            services.AddOktaCommon();
            return services;
        }

        /// <summary>
        /// Generic keyvault binding; requires custom cache (DOES NOT ADD ONE FOR YOU) (i.e. AddMemoryCache or AddDistributedMemoryCache or AddStackExchangeRedisCache or AddDistributedSqlServerCache) and optional HttpClient.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        /// <param name="configuration">wire up TokenService + filter, and allow optional HttpClient customization</param>
        /// <param name="configureCache"></param>
        /// <param name="OKTASectionName">Name of the section for OKTA; defaults to "Okta".</param>
        /// <param name="clientBuilder">HttpClient builder for HttpFactory</param>
        /// <returns>Service collection modified with a custom </returns>
        /// <exception cref="ArgumentNullException"></exception>

        public static IServiceCollection AddOktaFilter(this IServiceCollection services, IConfiguration configuration, Action<IServiceCollection> configureCache, Action<IHttpClientBuilder>? clientBuilder = null, string OKTASectionName = "Okta")
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            // 1) Bind & validate typed options
            AddOktaOptionsBound(services, configuration, OKTASectionName);

            // Expose validated OktaOptions.Value because TokenService takes OktaOptions (not IOptions<>)
            services.TryAddSingleton(serviceProvider => serviceProvider.GetRequiredService<IOptions<OktaOptions>>().Value);

            // 2) Caller supplies the distributed cache registration (no provider deps here)
            configureCache?.Invoke(services);

            // 3) Typed HttpClient for ITokenService with sane defaults; allow customization
            var httpClientBuilder = services.AddHttpClient<ITokenService, TokenService>()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });

            clientBuilder?.Invoke(httpClientBuilder);

            // 4) Common helpers and the closed-generic filter
            AddOktaCommon(services);

            return services;
        }
    }
}
