using Maurer.OktaFilter;
using Maurer.OktaFilter.Helpers;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using UnitTesting.Fixture;
using UnitTesting.Harness;

namespace UnitTesting.Assertions.Filter
{
    public class UsingAuthenticationFilter : AuthenticationFilterHarness
    {
        private ILogger<AuthenticationFilter<TokenService>> TokenServiceLogger { get; set; }

        public UsingAuthenticationFilter(AuthenticationFilterFixture Fixture) : base(Fixture)
        {
            TokenServiceLogger = new Mock<ILogger<AuthenticationFilter<TokenService>>>().Object;
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

            string? cachedValue = null;
            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            DistributedCacheEntryOptions? cachedOptions = null;

            cacheHelper.Setup(cache => cache.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(
                    Maurer.OktaFilter.Settings.OAUTHKEY,
                    It.IsAny<object>(),
                    It.IsAny<DistributedCacheEntryOptions>()))
                .Callback<string, object, DistributedCacheEntryOptions>((key, value, options) =>
                {
                    cachedValue = value as string;
                    cachedOptions = options;
                })
                .Returns(Task.CompletedTask);

            
            tokenService.Setup(service => service.GetToken()).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;

            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            Assert.Equal(1, nextCount);
            Assert.NotNull(cachedValue);
            Assert.Contains("\"access_token\":\"mocked_token\"", cachedValue); // serialized Token
            Assert.NotNull(cachedOptions);

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

            var cacheHelper = new Mock<IDistributedCacheHelper>();

            cacheHelper.Setup(cache => cache.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(true);

            var tokenService = new Mock<ITokenService>();
            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            tokenService.Verify(service => service.GetToken(), Times.Never);

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

            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            var calls = 0;
            
            cacheHelper.Setup(cache => cache.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);

            
            tokenService.Setup(s => s.GetToken()).ReturnsAsync(() =>
            {
                calls++;
                if (calls < 3) throw new InvalidOperationException("transient");
                return AuthenticationFilterFixture.SampleToken();
            });

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, TokenServiceLogger);
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

            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();

            cacheHelper.Setup(cache => cache.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);

            tokenService.Setup(service => service.GetToken()).ReturnsAsync(new Token { AccessToken = "" }); // invalid

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, TokenServiceLogger);
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

            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            var context = AuthenticationFilterFixture.MockExecutingContext(initialStatus: 401);
            var nextCount = 0;

            cacheHelper.Setup(cache => cache.Has(Maurer.OktaFilter.Settings.OAUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);


            tokenService.Setup(service => service.GetToken()).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            //Pre-set a 401 on the response BEFORE action executes.
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };
            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, TokenServiceLogger);
            var calls = 0;

            tokenService.Reset();
            tokenService.Setup(service => service.GetToken()).ReturnsAsync(() =>
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

        [Fact]
        public async Task CacheExpires_ThenFilterFetchesNewToken()
        {
            // preserve globals
            var OG_OAUTHKEY = Maurer.OktaFilter.Settings.OAUTHKEY;
            var OG_RETRIES = Maurer.OktaFilter.Settings.RETRIES;
            var OG_RETRYSLEEP = Maurer.OktaFilter.Settings.RETRYSLEEP;
            var OG_TOKENLIFETIME = Maurer.OktaFilter.Settings.TOKENLIFETIME;

            // minimal settings your filter Validate() expects for this path
            Maurer.OktaFilter.Settings.OAUTHKEY = "OKTA-TOKEN";
            Maurer.OktaFilter.Settings.RETRIES = "0";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "0";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "1"; // 1 minute TTL

            try
            {
                // fake time + real helper over fake IDistributedCache
                var clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
                IDistributedCache dist = new MockCache(clock);
                var cacheHelper = new DistributedCacheHelper(dist);

                // token service returns first, then second
                var tokenService = new Mock<ITokenService>();
                tokenService
                    .SetupSequence(service => service.GetToken())
                    .ReturnsAsync(new Token { AccessToken = "first-token", TokenType = "Bearer", ExpiresIn = "1800", Scope = "openid profile" })
                    .ReturnsAsync(new Token { AccessToken = "second-token", TokenType = "Bearer", ExpiresIn = "1800", Scope = "openid profile" });

                var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper, NullLogger<AuthenticationFilter<TokenService>>.Instance);

                // 1) first request: no cache → fetch and cache "first-token"
                var ctx1 = AuthenticationFilterFixture.MockExecutingContext();
                var nextCount = 0;
                ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(ctx1, 200)(); };

                await filter.OnActionExecutionAsync(ctx1, next);
                tokenService.Verify(s => s.GetToken(), Times.Once);

                var cached1 = await cacheHelper.Get(Maurer.OktaFilter.Settings.OAUTHKEY);
                Assert.NotNull(cached1);
                Assert.Contains("first-token", cached1!);

                // 2) before expiry: should be cache hit, no new fetch
                var ctx2 = AuthenticationFilterFixture.MockExecutingContext();
                ActionExecutionDelegate next2 = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(ctx2, 200)(); };
                await filter.OnActionExecutionAsync(ctx2, next2);

                tokenService.Verify(s => s.GetToken(), Times.Once); // still 1 call

                // 3) advance beyond TTL and call again → cache entry self-expires, filter refetches
                clock.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));

                var ctx3 = AuthenticationFilterFixture.MockExecutingContext();
                ActionExecutionDelegate next3 = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(ctx3, 200)(); };
                await filter.OnActionExecutionAsync(ctx3, next3);

                tokenService.Verify(s => s.GetToken(), Times.Exactly(2));
                var cached2 = await cacheHelper.Get(Maurer.OktaFilter.Settings.OAUTHKEY);
                Assert.NotNull(cached2);
                Assert.Contains("second-token", cached2!);

                Assert.Equal(3, nextCount); // next called once per request
            }
            finally
            {
                Maurer.OktaFilter.Settings.OAUTHKEY = OG_OAUTHKEY;
                Maurer.OktaFilter.Settings.RETRIES = OG_RETRIES;
                Maurer.OktaFilter.Settings.RETRYSLEEP = OG_RETRYSLEEP;
                Maurer.OktaFilter.Settings.TOKENLIFETIME = OG_TOKENLIFETIME;
            }
        }
    }
}
