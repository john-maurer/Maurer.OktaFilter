using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace UnitTesting.Assertions.Filter
{
    public class TestableAuthenticationFilter : AuthenticationFilter<TokenService>
    {
        public TestableAuthenticationFilter(ITokenService tokenService, IDistributedCacheHelper memoryCache, OktaOptions options, ILogger<AuthenticationFilter<TokenService>> logger) 
            : base(tokenService, memoryCache, options, logger)
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
