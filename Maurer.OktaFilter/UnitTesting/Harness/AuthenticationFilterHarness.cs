using Microsoft.AspNetCore.Mvc.Filters;
using UnitTesting.Assertions.Filter;
using UnitTesting.Fixture;

namespace UnitTesting.Harness
{
    public class AuthenticationFilterHarness : AbstractHarness<AuthenticationFilterFixture>
    {
        protected override async Task<int> Act(params object[] parameters)
        {
            var result = 0;
            var filter = (TestableAuthenticationFilter) parameters[0];
            var actionExecutingContext = (ActionExecutingContext)parameters[1];
            var actionExecutionDelegate = (ActionExecutionDelegate)parameters[2];

            await filter.OnActionExecutionAsync(actionExecutingContext, actionExecutionDelegate);

            result = filter.Counter;

            filter.Counter = 0;

            return result;
        }

        public AuthenticationFilterHarness(AuthenticationFilterFixture Fixture) : base(Fixture)
        {

        }
    }
}
