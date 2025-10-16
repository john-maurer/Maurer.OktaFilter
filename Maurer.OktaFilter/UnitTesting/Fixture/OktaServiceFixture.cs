using Maurer.OktaFilter.Models;
using Maurer.OktaFilter.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Net;

namespace UnitTesting.Fixture
{
    public class OktaServiceFixture : AbstractFixture
    {
        private MockHttpMessageHandler MockMessageHandler(HttpStatusCode status, string? payload) =>
            new MockHttpMessageHandler((request, token) =>
            {
                HttpResponseMessage result;

                if (request!.RequestUri!.AbsoluteUri == Options.AUTHURL)
                {
                    var response = new HttpResponseMessage(status);
                    response.Content = new StringContent(payload!);
                    result = response;
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.NotFound);
                    response.Content = new StringContent($"Bad set-up in OKTA service fixture; the target URL does not match '{Options.AUTHURL}'.");
                    result = response;
                }

                return Task.FromResult(result);
            });

        protected override void Arrange(params object[] parameters)
        {
            var mockLogger = new Mock<ILogger<TokenService>>();
            var token = new OktaToken { 
                AccessToken = "mocked_token",
                ExpiresIn = Options.LIFETIME.ToString(),
                Scope = Options.SCOPE,
                TokenType = "bearer"
            };

            var mockClientOK = new HttpClient(MockMessageHandler(HttpStatusCode.OK, JsonConvert.SerializeObject(token)));
            var mockClientUnauthorized = new HttpClient(MockMessageHandler(HttpStatusCode.Unauthorized, "Unauthorized: Invalid authentication credentials."));
            var mockClientForbidden = new HttpClient(MockMessageHandler(HttpStatusCode.Forbidden, "Forbidden: Insufficient permissions to access this resource."));
            var mockClientProxyRequired = new HttpClient(MockMessageHandler((HttpStatusCode)407, "Proxy Authentication Required: Unable to authenticate with proxy server."));

            ContextServiceOK = new TokenService(mockClientOK, Options, mockLogger.Object);
            ContextServiceUnauthorized = new TokenService(mockClientUnauthorized, Options, mockLogger.Object);
            ContextServiceForbidden = new TokenService(mockClientForbidden, Options, mockLogger.Object);
            ContextServiceProxyRequired = new TokenService(mockClientProxyRequired, Options, mockLogger.Object);
        }

        public OktaServiceFixture() => Arrange();

        public OktaOptions Options { get; set; } = new OktaOptions
        {
            AUTHURL = "https://mockoauthserver.com/token",
            USER = "testuser",
            PASSWORD = "testpassword",
            AUTHKEY = "OKTA-TOKEN",
            GRANT = "client_credentials",
            SCOPE = "openid profile email",
            SLEEP = 30,
            RETRIES = 3,
            LIFETIME = 30,
        };

        public TokenService ContextServiceOK { get; set; }
        public TokenService ContextServiceUnauthorized{ get; set; }
        public TokenService ContextServiceForbidden { get; set; }
        public TokenService ContextServiceProxyRequired { get; set; }
    }
}