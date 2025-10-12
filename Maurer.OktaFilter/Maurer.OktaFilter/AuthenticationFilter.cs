using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
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
        private readonly ITokenService _tokenService;
        private readonly AsyncRetryPolicy<IActionResult> _retryPolicy;
        private readonly IDistributedCacheHelper _memoryCache;
        private readonly OktaOptions _options;

        private DistributedCacheEntryOptions BuildCacheOptions() =>
            new () { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Convert.ToInt32(_options.LIFETIME)) };

        public AuthenticationFilter( ITokenService tokenService, IDistributedCacheHelper memoryCache, OktaOptions options, ILogger<AuthenticationFilter<TokenService>> logger)
        {
            _logger = logger;
            _tokenService = tokenService;
            _memoryCache = memoryCache;
            _options = options;

            _retryPolicy = Policy
                .Handle<Exception>()
                .OrResult<IActionResult>(response => IsAuthenticationFailure(response))
                .RetryAsync(
                    retryCount: Convert.ToInt32(_options.RETRIES),
                    onRetryAsync: async (_, retryNumber) =>
                    {
                        _logger.LogWarning("Starting attempt #{Attempt} at re-authenticating...", retryNumber);

                        var sleep = Convert.ToInt32(_options.SLEEP);

                        if (sleep > 0)
                            await Task.Delay(TimeSpan.FromSeconds(sleep));
                    }
                );
        }

        virtual protected bool IsAuthenticationFailure(IActionResult? result) =>
            result switch
            {
                ObjectResult obj when obj.StatusCode is 401 or 403 or 407 => true,
                StatusCodeResult status when status.StatusCode is 401 or 403 or 407 => true,
                ForbidResult => true,
                UnauthorizedResult => true,
                _ => false
            };

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                if (!await _memoryCache.Has(_options.OAUTHKEY))
                {
                    await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var token = await _tokenService.GetToken();

                        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
                            throw new InvalidOperationException("Failed to acquire OKTA token.");

                        await _memoryCache.Set(_options.OAUTHKEY, JsonConvert.SerializeObject(token), BuildCacheOptions());

                        return new StatusCodeResult(context.HttpContext.Response.StatusCode);
                    });
                }

                var execution = await next();

            }
            catch
            {
                throw;
            }
        }
    }
}
