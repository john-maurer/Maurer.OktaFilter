using UnitTesting.Fixture;

namespace UnitTesting.Harness
{
    public class AuthenticationFilterHarness : AbstractHarness<AuthenticationFilterFixture>
    {
        protected override async Task Act(params object[] parameters)
        {
            
        }

        public AuthenticationFilterHarness(AuthenticationFilterFixture Fixture) : base(Fixture)
        {

        }
    }
}
