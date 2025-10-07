using Maurer.OktaFilter;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using UnitTesting.Fixture;
using UnitTesting.Harness;

namespace UnitTesting.Assertions.Filter
{
    public class UsingAuthenticationFilter : AuthenticationFilterHarness
    {
        public UsingAuthenticationFilter(AuthenticationFilterFixture Fixture) : base(Fixture)
        {
        }

        [Fact]
        public async Task EnsuresTokenOnCacheMiss_ThenCallsNextOnce()
        {
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "0";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "30";

            var cache = new Mock<IDistributedCacheHelper>();
            cache.Setup(c => c.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);

            string? cachedValue = null;
            DistributedCacheEntryOptions? cachedOpts = null;
            cache.Setup(c => c.Set(
                    Maurer.OktaFilter.Settings.OAUTHKEY,
                    It.IsAny<object>(),
                    It.IsAny<DistributedCacheEntryOptions>()))
                .Callback<string, object, DistributedCacheEntryOptions>((k, v, opt) =>
                {
                    cachedValue = v as string;
                    cachedOpts = opt;
                })
                .Returns(Task.CompletedTask);

            var tokenSvc = new Mock<ITokenService>();
            tokenSvc.Setup(s => s.GetToken()).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            var logger = new Mock<ILogger<AuthenticationFilter<TokenService>>>();

            var filter = new TestableAuthenticationFilter(tokenSvc.Object, cache.Object, logger.Object);

            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            Assert.Equal(1, nextCount);
            Assert.NotNull(cachedValue);
            Assert.Contains("\"access_token\":\"mocked_token\"", cachedValue); // serialized Token
            Assert.NotNull(cachedOpts);

            Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
            Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
            Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
            Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
        }

        [Fact]
        public async Task DoesNotFetchTokenOnCacheHit_CallsNextOnce()
        {
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "0";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "30";

            var cache = new Mock<IDistributedCacheHelper>();
            cache.Setup(c => c.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(true);

            var tokenSvc = new Mock<ITokenService>();
            var logger = new Mock<ILogger<AuthenticationFilter<TokenService>>>();
            var filter = new TestableAuthenticationFilter(tokenSvc.Object, cache.Object, logger.Object);

            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            tokenSvc.Verify(s => s.GetToken(), Times.Never);
            Assert.Equal(1, nextCount);

            Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
            Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
            Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
            Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
        }

        [Fact]
        public async Task RetriesTokenAcquisition_WhenServiceThrows_ThenSucceeds()
        {
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "2";      // 2 retries => 3 total attempts
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "30";

            var cache = new Mock<IDistributedCacheHelper>();
            cache.Setup(c => c.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);
            cache.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);

            var calls = 0;
            var tokenSvc = new Mock<ITokenService>();
            tokenSvc.Setup(s => s.GetToken()).ReturnsAsync(() =>
            {
                calls++;
                if (calls < 3) throw new InvalidOperationException("transient");
                return AuthenticationFilterFixture.SampleToken();
            });

            var logger = new Mock<ILogger<AuthenticationFilter<TokenService>>>();
            var filter = new TestableAuthenticationFilter(tokenSvc.Object, cache.Object, logger.Object);

            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            Assert.Equal(3, calls); // 1 try + 2 retries
            Assert.Equal(1, nextCount);

            Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
            Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
            Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
            Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
        }

        [Fact]
        public async Task ThrowsWhenTokenInvalid_AndDoesNotCallNext()
        {
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "0";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "30";

            var cache = new Mock<IDistributedCacheHelper>();
            cache.Setup(c => c.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);

            var tokenSvc = new Mock<ITokenService>();
            tokenSvc.Setup(s => s.GetToken()).ReturnsAsync(new Token { AccessToken = "" }); // invalid

            var logger = new Mock<ILogger<AuthenticationFilter<TokenService>>>();
            var filter = new TestableAuthenticationFilter(tokenSvc.Object, cache.Object, logger.Object);

            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await Assert.ThrowsAsync<InvalidOperationException>(() => filter.OnActionExecutionAsync(context, next));
            Assert.Equal(0, nextCount);

            Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
            Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
            Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
            Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
        }

        [Fact]
        public async Task PreAction401CausesSpuriousRetries_DueToResultPredicate()
        {
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "2";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "30";

            var cache = new Mock<IDistributedCacheHelper>();
            cache.Setup(c => c.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);
            cache.Setup(c => c.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);

            var tokenSvc = new Mock<ITokenService>();
            tokenSvc.Setup(s => s.GetToken()).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            var logger = new Mock<ILogger<AuthenticationFilter<TokenService>>>();
            var filter = new TestableAuthenticationFilter(tokenSvc.Object, cache.Object, logger.Object);

            //Pre-set a 401 on the response BEFORE action executes.
            var context = AuthenticationFilterFixture.MockExecutingContext(initialStatus: 401);
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            var calls = 0;
            tokenSvc.Reset();
            tokenSvc.Setup(s => s.GetToken()).ReturnsAsync(() =>
            {
                calls++;
                return AuthenticationFilterFixture.SampleToken();
            });

            await filter.OnActionExecutionAsync(context, next);

            //Expecting token fetched once, next called once.
            //Reality (bug): result predicate sees 401 and retries token acquisition.
            Assert.True(calls > 1); //exposes the bug
            Assert.Equal(1, nextCount);

            Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
            Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
            Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
            Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
        }
    }
}
