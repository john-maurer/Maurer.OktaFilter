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
            await Record.ExceptionAsync(() => Act(_fixture.ContextServiceOK));
        

        [Fact]
        async public Task ShouldReturnTokenOnStatusOK()
        {
            object? result = await Act(_fixture.ContextServiceOK);

            Assert.NotNull(result);
            Assert.IsType<Token>(result);
        }

        [Fact]
        async public Task ShouldHandleStatusUnauthorizedWithoutException() =>
            await Record.ExceptionAsync(() => Act(_fixture.ContextServiceUnauthorized));

        [Fact]
        async public Task ShouldReturnNullOnStatusUnauthorized()
        {
            object? result = await Act(_fixture.ContextServiceUnauthorized);

            Assert.Null(result);
        }

        [Fact]
        async public Task ShouldHandleStatusForbiddenWithoutException() =>
            await Record.ExceptionAsync(() => Act(_fixture.ContextServiceForbidden));

        [Fact]
        async public Task ShouldReturnNullOnStatusForbidden()
        {
            object? result = await Act(_fixture.ContextServiceForbidden);

            Assert.Null(result);
        }

        [Fact]
        async public Task ShouldHandleStatusProxyRequiredWithoutException() =>
            await Record.ExceptionAsync(() => Act(_fixture.ContextServiceProxyRequired));

        [Fact]
        async public Task ShouldReturnNullOnStatusProxyRequired()
        {
            object? result = await Act(_fixture.ContextServiceProxyRequired);

            Assert.Null(result);
        }
    }
}
