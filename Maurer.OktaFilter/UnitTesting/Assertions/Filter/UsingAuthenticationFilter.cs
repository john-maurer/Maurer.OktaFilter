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
            string? cachedValue = null;
            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            DistributedCacheEntryOptions? cachedOptions = null;

            cacheHelper.Setup(cache => cache.Has(_fixture.Options.AUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(
                    _fixture.Options.AUTHKEY,
                    It.IsAny<object>(),
                    It.IsAny<DistributedCacheEntryOptions>()))
                .Callback<string, object, DistributedCacheEntryOptions>((key, value, options) =>
                {
                    cachedValue = value as string;
                    cachedOptions = options;
                })
                .Returns(Task.CompletedTask);

            
            tokenService.Setup(service => service.GetToken(CancellationToken.None)).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, _fixture.Options, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;

            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            Assert.Equal(1, nextCount);
            Assert.NotNull(cachedValue);
            Assert.Contains("\"access_token\":\"mocked_token\"", cachedValue); // serialized Token
            Assert.NotNull(cachedOptions);
        }

        [Fact]
        public async Task DoesNotFetchTokenOnCacheHit_CallsNextOnce()
        {
            var cacheHelper = new Mock<IDistributedCacheHelper>();

            cacheHelper.Setup(cache => cache.Has(_fixture.Options.AUTHKEY)).ReturnsAsync(true);

            var tokenService = new Mock<ITokenService>();
            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, new OktaOptions { 
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = _fixture.Options.OAUTHURL,
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = _fixture.Options.LIFETIME,
                RETRIES = 0,
                SLEEP = 0
            }, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            tokenService.Verify(service => service.GetToken(CancellationToken.None), Times.Never);

            Assert.Equal(1, nextCount);
        }

        [Fact]
        public async Task RetriesTokenAcquisition_WhenServiceThrows_ThenSucceeds()
        {
            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            var calls = 0;
            
            cacheHelper.Setup(cache => cache.Has(_fixture.Options.AUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);

            
            tokenService.Setup(s => s.GetToken(CancellationToken.None)).ReturnsAsync(() =>
            {
                calls++;
                if (calls < 3) throw new InvalidOperationException("transient");
                return AuthenticationFilterFixture.SampleToken();
            });

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, new OktaOptions
            {
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = _fixture.Options.OAUTHURL,
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = _fixture.Options.LIFETIME,
                RETRIES = 2,
                SLEEP = 0
            }, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await filter.OnActionExecutionAsync(context, next);

            Assert.Equal(3, calls); // 1 try + 2 retries
            Assert.Equal(1, nextCount);
        }

        [Fact]
        public async Task ThrowsWhenTokenInvalid_AndDoesNotCallNext()
        {
            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();

            cacheHelper.Setup(cache => cache.Has(_fixture.Options.AUTHKEY)).ReturnsAsync(false);

            tokenService.Setup(service => service.GetToken(CancellationToken.None)).ReturnsAsync(new OktaToken { AccessToken = "" }); // invalid

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, new OktaOptions
            {
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = _fixture.Options.OAUTHURL,
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = _fixture.Options.LIFETIME,
                RETRIES = 0,
                SLEEP = 0
            }, TokenServiceLogger);
            var context = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };

            await Assert.ThrowsAsync<InvalidOperationException>(() => filter.OnActionExecutionAsync(context, next));

            Assert.Equal(0, nextCount);
        }

        [Fact]
        public async Task PreAction401CausesSpuriousRetries_DueToResultPredicate()
        {
            var cacheHelper = new Mock<IDistributedCacheHelper>();
            var tokenService = new Mock<ITokenService>();
            var context = AuthenticationFilterFixture.MockExecutingContext(initialStatus: 401);
            var nextCount = 0;

            cacheHelper.Setup(cache => cache.Has(_fixture.Options.AUTHKEY)).ReturnsAsync(false);
            cacheHelper.Setup(cache => cache.Set(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DistributedCacheEntryOptions>()))
                 .Returns(Task.CompletedTask);


            tokenService.Setup(service => service.GetToken(CancellationToken.None)).ReturnsAsync(AuthenticationFilterFixture.SampleToken());

            //Pre-set a 401 on the response BEFORE action executes.
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context, 200)(); };
            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper.Object, new OktaOptions
            {
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = _fixture.Options.OAUTHURL,
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = _fixture.Options.LIFETIME,
                RETRIES = 2,
                SLEEP = 0
            }, TokenServiceLogger);
            var calls = 0;

            tokenService.Reset();
            tokenService.Setup(service => service.GetToken(CancellationToken.None)).ReturnsAsync(() =>
            {
                calls++;
                return AuthenticationFilterFixture.SampleToken();
            });

            await filter.OnActionExecutionAsync(context, next);

            //Expecting token fetched once, next called once.
            //Reality (bug): result predicate sees 401 and retries token acquisition.
            Assert.True(calls > 1); //exposes the bug
            Assert.Equal(1, nextCount);
        }

        [Fact]
        public async Task CacheExpires_ThenFilterFetchesNewToken()
        {
            // fake time + real helper over fake IDistributedCache
            var clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var cacheHelper = new DistributedCacheHelper(new MockCache(clock));

            // token service returns first, then second
            var tokenService = new Mock<ITokenService>();
            tokenService
                .SetupSequence(service => service.GetToken(CancellationToken.None))
                .ReturnsAsync(new OktaToken { AccessToken = "first-token", TokenType = "Bearer", ExpiresIn = "0", Scope = _fixture.Options.SCOPE })
                .ReturnsAsync(new OktaToken { AccessToken = "second-token", TokenType = "Bearer", ExpiresIn = "0", Scope = _fixture.Options.SCOPE });

            var filter = new TestableAuthenticationFilter(tokenService.Object, cacheHelper, new OktaOptions
            {
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = _fixture.Options.OAUTHURL,
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = 1,
                RETRIES = 0,
                SLEEP = 1
            }, NullLogger<AuthenticationFilter<TokenService>>.Instance);

            // 1) first request: no cache → fetch and cache "first-token"
            var context1 = AuthenticationFilterFixture.MockExecutingContext();
            var nextCount = 0;
            ActionExecutionDelegate next = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context1, 200)(); };

            await filter.OnActionExecutionAsync(context1, next);
            tokenService.Verify(s => s.GetToken(CancellationToken.None), Times.Once);

            var cached1 = await cacheHelper.Get(_fixture.Options.AUTHKEY);
            Assert.NotNull(cached1);
            Assert.Contains("first-token", cached1!);

            // 2) before expiry: should be cache hit, no new fetch
            var context2 = AuthenticationFilterFixture.MockExecutingContext();
            ActionExecutionDelegate next2 = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context2, 200)(); };
            await filter.OnActionExecutionAsync(context2, next2);

            tokenService.Verify(s => s.GetToken(CancellationToken.None), Times.Once); // still 1 call

            // 3) advance beyond TTL and call again → cache entry self-expires, filter refetches
            clock.Advance(TimeSpan.FromMinutes(1).Add(TimeSpan.FromSeconds(1)));

            var context3 = AuthenticationFilterFixture.MockExecutingContext();
            ActionExecutionDelegate next3 = async () => { nextCount++; return await AuthenticationFilterFixture.MockNext(context3, 200)(); };
            await filter.OnActionExecutionAsync(context3, next3);

            tokenService.Verify(service => service.GetToken(CancellationToken.None), Times.Exactly(2));
            var cached2 = await cacheHelper.Get(_fixture.Options.AUTHKEY);
            Assert.NotNull(cached2);
            Assert.Contains("second-token", cached2!);

            Assert.Equal(3, nextCount); // next called once per request
        }
    }
}
