using Maurer.OktaFilter.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace Maurer.OktaFilter
{
    public class AuthenticationFilter<TokenService> : IAsyncActionFilter
        where TokenService : ITokenService
    {
        private readonly ILogger<AuthenticationFilter<TokenService>> _logger;
        private readonly TokenService _tokenService;
        private readonly AsyncRetryPolicy<IActionResult> _retryPolicy;
        private readonly IDistributedCacheHelper _memoryCache;
        private readonly DistributedCacheEntryOptions _cacheOptions;

        public AuthenticationFilter(
            TokenService tokenService,
            IDistributedCacheHelper memoryCache,
            ILogger<AuthenticationFilter<TokenService>> logger)
        {
            _logger = logger;
            _tokenService = tokenService;
            _memoryCache = memoryCache;
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(Convert.ToInt32(Settings.TOKENLIFETIME)),
                SlidingExpiration = TimeSpan.FromMinutes(Convert.ToInt32(Settings.TOKENLIFETIME)),
            };

            _retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<IActionResult>(response => IsAuthenticationError((ObjectResult)response))
                .RetryAsync(Convert.ToInt32(Settings.RETRIES), async (_, retryCount) =>
                {
                    _logger.LogWarning("Starting attempt #{0} at re-authenticating...", retryCount);
                    await Task.Delay(Convert.ToInt32(Settings.RETRYSLEEP));
                    var token = await _tokenService.GetToken();
                    await _memoryCache.Set("OKTA-TOKEN", token!.AccessToken, _cacheOptions);
                });
        }

        virtual protected bool IsAuthenticationError(ObjectResult? result) =>
            result?.StatusCode is 401 or 403 or 407;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (!await _memoryCache.Has("OKTA-TOKEN"))
                    {
                        var token = await _tokenService.GetToken();
                        await _memoryCache.Set("OKTA-TOKEN", JsonConvert.SerializeObject(token), _cacheOptions);
                    }

                    var result = (await next()).Result as ObjectResult;

                    return result!;
                });
            }
            catch
            {
                throw;
            }
        }
    }
}
