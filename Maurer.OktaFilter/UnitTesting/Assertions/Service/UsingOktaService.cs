using Maurer.OktaFilter.Models;
using UnitTesting.Fixture;
using UnitTesting.Harness;

namespace UnitTesting.Assertions.Service
{
    public class UsingOktaService : OktaServiceHarness
    {
        public UsingOktaService(OktaServiceFixture filter) : base(filter)
        {

        }

        [Fact]
        async public Task ShouldHandleStatusOKWithoutException() =>
            await Record.ExceptionAsync(() => _fixture.ContextOK.GetToken());
        

        [Fact]
        async public Task ShouldReturnTokenOnStatusOK()
        {
            object? result = await _fixture.ContextOK.GetToken();

            Assert.NotNull(result);
            Assert.IsType<Token>(result);
        }

        [Fact]
        async public Task ShouldHandleStatusUnauthorizedWithoutException() =>
            await Record.ExceptionAsync(() => _fixture.ContextUnauthorized.GetToken());

        [Fact]
        async public Task ShouldReturnNullOnStatusUnauthorized()
        {
            object? result = await _fixture.ContextUnauthorized.GetToken();

            Assert.Null(result);
        }

        [Fact]
        async public Task ShouldHandleStatusForbiddenWithoutException() =>
            await Record.ExceptionAsync(() => _fixture.ContextForbidden.GetToken());

        [Fact]
        async public Task ShouldReturnNullOnStatusForbidden()
        {
            object? result = await _fixture.ContextForbidden.GetToken();

            Assert.Null(result);
        }

        [Fact]
        async public Task ShouldHandleStatusProxyRequiredWithoutException() =>
            await Record.ExceptionAsync(() => _fixture.ContextProxyRequired.GetToken());

        [Fact]
        async public Task ShouldReturnNullOnStatusProxyRequired()
        {
            object? result = await _fixture.ContextProxyRequired.GetToken();

            Assert.Null(result);
        }
    }
}
