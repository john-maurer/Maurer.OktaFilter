using UnitTesting.Fixture;

namespace UnitTesting.Harness
{
    public class AuthenticationFilterHarness : AbstractHarness<AuthenticationFilterFixture>
    {
        protected override Task Act(params object[] parameters) => Task.CompletedTask;
        

        public AuthenticationFilterHarness(AuthenticationFilterFixture Fixture) : base(Fixture)
        {

        }
    }
}
