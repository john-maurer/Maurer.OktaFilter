using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UnitTesting.Fixture;
using UnitTesting.Harness;

namespace UnitTesting.Assertions
{
    public class UsingOktaService : OktaServiceHarness
    {
        private static OktaToken OkBody() => new OktaToken
        {
            AccessToken = "abc123",
            TokenType = "Bearer",
            ExpiresIn = "1800",
            Scope = "openid profile"
        };

        private MockHttpMessageHandler GetMockedResponseWithStringContent(string content)
        {
            var handler = new MockHttpMessageHandler((request, context) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(content)
                };

                return Task.FromResult(response);

            });

            return handler;
        }

        public UsingOktaService(OktaServiceFixture filter) : base(filter)
        {

        }

        public TokenService MakeService(Action<HttpRequestMessage>? capture = null)
        {
            var handler = new MockHttpMessageHandler((request, cancellationToken) =>
            {
                capture?.Invoke(request);

                var body = JsonConvert.SerializeObject(OkBody());
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };

                return Task.FromResult(response); // Task<HttpResponseMessage>
            });

            var logger = new Mock<ILogger<TokenService>>();
            var client = new HttpClient(handler);
            return new TokenService(client, _fixture.Options, logger.Object);
        }

        [Fact]
        public async Task ShouldTargetConfiguredOAuthUrl()
        {
            Uri? requestUri = null;
            var service = MakeService(request => requestUri = request.RequestUri);

            await service.GetToken();

            Assert.Equal(new Uri(_fixture.Options.OAUTHURL), requestUri);
        }

        [Fact]
        public async Task ShouldUseBasicAuthScheme()
        {
            string? scheme = null;
            var service = MakeService(request => scheme = request.Headers.Authorization?.Scheme);

            await service.GetToken();

            Assert.Equal("Basic", scheme);
        }

        [Fact]
        public async Task ShouldSendCorrectBasicCredentials()
        {
            string? param = null;
            var service = MakeService(request => param = request.Headers.Authorization?.Parameter);

            await service.GetToken();

            var expected = Convert.ToBase64String
                (Encoding.UTF8.GetBytes($"{_fixture.Options.USER}:{_fixture.Options.PASSWORD}"));

            Assert.Equal(expected, param);
        }

        [Fact]
        public async Task ShouldSetAcceptHeaderToApplicationJson()
        {
            string? accept = null;
            var service = MakeService(request => accept = request.Headers.Accept.FirstOrDefault()?.MediaType);

            await service.GetToken();

            Assert.Equal("application/json", accept);
        }

        [Fact]
        public async Task ShouldPreferHttp2OnRequest()
        {
            Version? requestUri = null;
            var service = MakeService(request => requestUri = request.Version);

            await service.GetToken();

            Assert.Equal(2, requestUri!.Major);
        }

        [Fact]
        public async Task ShouldUseFormUrlEncodedContentType()
        {
            string? contentType = null;
            var service = MakeService(request => contentType = request.Content?.Headers.ContentType?.MediaType);

            await service.GetToken();

            Assert.Equal("application/x-www-form-urlencoded", contentType);
        }

        [Fact]
        public async Task ShouldIncludeGrantTypeInForm()
        {
            string? form = null;
            var service = MakeService(async request => form = await request.Content!.ReadAsStringAsync());

            await service.GetToken();

            Assert.Contains($"grant_type={Uri.EscapeDataString(_fixture.Options.GRANT)}", form);
        }

        [Fact]
        public async Task ShouldIncludeScopeInForm()
        {
            string? form = null;
            var service = MakeService(async request => form = await request.Content!.ReadAsStringAsync());

            await service.GetToken();

            // FormUrlEncodedContent encodes spaces as '+'
            var expectedScope = _fixture.Options.SCOPE.Replace(" ", "+");
            Assert.Contains($"scope={expectedScope}", form);
        }

        [Fact]
        public async Task ShouldReturnDeserializedTokenOnStatusOk()
        {
            OktaToken? token = await Act(MakeService());

            Assert.NotNull(token);
        }

        [Fact]
        public async Task ShouldReturnNullOnStatusOkWithWhitespaceBody()
        {
            var logger = new Mock<ILogger<TokenService>>();
            var client = new HttpClient(GetMockedResponseWithStringContent("   \t  \r\n"));
            var service = new TokenService(client, _fixture.Options, logger.Object);

            var token = await service.GetToken();

            Assert.Null(token);
        }

        [Fact]
        public async Task ShouldReturnNullOnStatusOkWithMalformedJson()
        {
            var logger = new Mock<ILogger<TokenService>>();
            var client = new HttpClient(GetMockedResponseWithStringContent("this is not json"));
            var service = new TokenService(client, _fixture.Options, logger.Object);

            var token = await service.GetToken();

            Assert.Null(token);
        }



        [Fact]
        async public Task ShouldHandleStatusOKWithoutException() =>
            await Record.ExceptionAsync(() => Act(_fixture.ContextServiceOK));
        

        [Fact]
        async public Task ShouldReturnTokenOnStatusOK()
        {
            object? result = await Act(_fixture.ContextServiceOK);

            Assert.NotNull(result);
            Assert.IsType<OktaToken>(result);
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

        [Fact]
        public async Task ShouldThrowOnNonHttpsUrl()
        {
            var handler = new MockHttpMessageHandler((request, cancellationToken) =>
            {
                // This should never be hit because the method throws before sending
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            });

            var logger = new Mock<ILogger<TokenService>>();
            var client = new HttpClient(handler);
            var service = new TokenService(client, new OktaOptions
            {
                USER = _fixture.Options.USER,
                PASSWORD = _fixture.Options.PASSWORD,
                OAUTHURL = "http://mockoauthserver.com/token", // Force non-HTTPS to trigger the guard
                AUTHKEY = _fixture.Options.AUTHKEY,
                GRANT = _fixture.Options.GRANT,
                SCOPE = _fixture.Options.SCOPE,
                LIFETIME = _fixture.Options.LIFETIME,
                RETRIES = 0,
                SLEEP = 0
            }, logger.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetToken());
        }
    }
}
