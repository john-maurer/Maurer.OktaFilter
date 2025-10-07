using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace UnitTesting.Assertions.Filter
{
    public class TestableAuthenticationFilter : AuthenticationFilter<TokenService>
    {
        public TestableAuthenticationFilter(ITokenService tokenService, IDistributedCacheHelper memoryCache, ILogger<AuthenticationFilter<TokenService>> logger) 
            : base(tokenService, memoryCache, logger)
        {
            Counter = 0;
        }

        public int Counter { get; set; }

        protected override bool IsAuthenticationFailure(IActionResult? actionResult)
        {
            var result = base.IsAuthenticationFailure(actionResult);

            if (result) Counter++;

            return result;
        }
    }
}
