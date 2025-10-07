using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTesting.Assertions.Filter;

namespace UnitTesting.Fixture
{
    public class AuthenticationFilterFixture : AbstractFixture
    {
        protected override void Arrange(params object[] parameters)
        {
            
        }

        public TestableAuthenticationFilter GetAuthenticationFilterContext(TokenService tokenService, IDistributedCacheHelper memoryCache) =>
            new TestableAuthenticationFilter(tokenService, memoryCache, new Mock<ILogger<AuthenticationFilter<TokenService>>>().Object);

        public static Token SampleToken() => new Token
        {
            AccessToken = "mocked_token",
            TokenType = "Bearer",
            ExpiresIn = "1800",
            Scope = "openid profile"
        };

        public static ActionExecutingContext MockExecutingContext(int initialStatus = 200)
        {
            var http = new DefaultHttpContext();
            http.Response.StatusCode = initialStatus;

            var actionContext = new ActionContext(http, new RouteData(), new ControllerActionDescriptor());

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        public static ActionExecutionDelegate MockNext(ActionExecutingContext execCtx, int statusCode = 200)
        {
            return () =>
            {
                var executed = new ActionExecutedContext(
                    new ActionContext(execCtx.HttpContext, execCtx.RouteData, execCtx.ActionDescriptor),
                    new List<IFilterMetadata>(),
                    controller: new object())
                {
                    Result = new ObjectResult("ok") { StatusCode = statusCode }
                };

                return Task.FromResult(executed);
            };
        }
    }
}
