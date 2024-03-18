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

                if (request!.RequestUri!.AbsoluteUri == Maurer.OktaFilter.Settings.OAUTHURL)
                {
                    var response = new HttpResponseMessage(status);
                    response.Content = new StringContent(payload!);
                    result = response;
                }
                else
                {
                    var response = new HttpResponseMessage(HttpStatusCode.NotFound);
                    response.Content = new StringContent($"Bad set-up in OKTA service fixture; the target URL does not match '{Maurer.OktaFilter.Settings.OAUTHURL}'.");
                    result = response;
                }

                return Task.FromResult(result);
            });

        protected override void Arrange(params object[] parameters)
        {
            Maurer.OktaFilter.Settings.OAUTHURL = "https://mockoauthserver.com/token";
            Maurer.OktaFilter.Settings.OAUTHUSER = "testuser";
            Maurer.OktaFilter.Settings.OAUTHPASSWORD = "testpassword";
            Maurer.OktaFilter.Settings.GRANTTYPE = "client_credentials";
            Maurer.OktaFilter.Settings.SCOPE = "openid profile email";
            Maurer.OktaFilter.Settings.RETRYSLEEP = "30";
            Maurer.OktaFilter.Settings.RETRIES = "3";
            Maurer.OktaFilter.Settings.TOKENLIFETIME = "300";

            var mockLogger = new Mock<ILogger<TokenService>>();
            var token = new Token { 
                AccessToken = "mocked_token",
                ExpiresIn = Maurer.OktaFilter.Settings.TOKENLIFETIME,
                Scope = Maurer.OktaFilter.Settings.SCOPE,
                TokenType = "bearer"
            };

            var mockClientOK = new HttpClient(MockMessageHandler(HttpStatusCode.OK, JsonConvert.SerializeObject(token)));
            var mockClientUnauthorized = new HttpClient(MockMessageHandler(HttpStatusCode.Unauthorized, "Unauthorized: Invalid authentication credentials."));
            var mockClientForbidden = new HttpClient(MockMessageHandler(HttpStatusCode.Forbidden, "Forbidden: Insufficient permissions to access this resource."));
            var mockClientProxyRequired = new HttpClient(MockMessageHandler((HttpStatusCode)407, "Proxy Authentication Required: Unable to authenticate with proxy server."));

            ContextOK = new TokenService(mockLogger.Object, mockClientOK);
            ContextUnauthorized = new TokenService(mockLogger.Object, mockClientUnauthorized);
            ContextForbidden = new TokenService(mockLogger.Object, mockClientForbidden);
            ContextProxyRequired = new TokenService(mockLogger.Object, mockClientProxyRequired);
        }

        public OktaServiceFixture() => Arrange();

        public TokenService ContextOK { get; set; }
        public TokenService ContextUnauthorized{ get; set; }
        public TokenService ContextForbidden { get; set; }
        public TokenService ContextProxyRequired { get; set; }
    }
}