using UnitTesting.Fixture;

namespace UnitTesting.Harness
{
    public class ExtensionsHarness : AbstractHarness<ExtensionsFixture>
    {
        protected override Task Act(params object[] parameters) => Task.CompletedTask;
        
        public ExtensionsHarness(ExtensionsFixture Fixture) : base(Fixture)
        {

        }
    }
}
