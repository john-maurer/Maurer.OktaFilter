using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace UnitTesting.Assertions.Filter
{
    public class TestableAuthenticationFilter : AuthenticationFilter<TokenService>
    {
        public TestableAuthenticationFilter(TokenService tokenService, IDistributedCacheHelper memoryCache, ILogger<AuthenticationFilter<TokenService>> logger) 
            : base(tokenService, memoryCache, logger)
        {
            Counter = 0;
        }

        public int Counter { get; set; }

        override protected bool IsAuthenticationError(ObjectResult? resultObj)
        {
            var result = base.IsAuthenticationError(resultObj);

            if (result) Counter++;

            return result;
        }
    }
}
