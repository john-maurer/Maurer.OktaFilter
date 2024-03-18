using UnitTesting.Fixture;
using Xunit.Abstractions;

namespace UnitTesting.Harness
{
    /// <summary>
    /// Partially specialized test utility that couples a fixture to an inherited assertion class.
    /// </summary>
    /// <typeparam name="TFixture"></typeparam>

    public abstract class AbstractHarness<TFixture> : IClassFixture<TFixture>
        where TFixture : AbstractFixture
    {
        /// <summary>
        /// Encapsulated instance of a concrete 'AbstractFixture' representing a persistent resource of a unit under test
        /// </summary>

        protected readonly TFixture _fixture;

        /// <summary>
        /// Writes to the Test Explorer
        /// </summary>

        protected ITestOutputHelper _outputHelper;

        /// <summary>
        /// Interface for defining an action taken on a unit under test
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>

        protected abstract Task Act(params object[] parameters);

        /// <summary>
        /// Non-public assignment constructor imposing the requirement of handing a concrete fixture over to the framework
        /// </summary>
        /// <param name="Fixture">Concrete fixture implementation</param>

        protected AbstractHarness(TFixture Fixture) => _fixture = Fixture;
    }
}
