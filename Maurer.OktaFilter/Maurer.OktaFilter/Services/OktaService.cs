using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Maurer.OktaFilter.Services
{
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly HttpClient _httpClient;

        public TokenService(ILogger<TokenService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Token?> GetToken()
        {
            Token? token;

            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Settings.OAUTHUSER}:{Settings.OAUTHPASSWORD}"));
                var tokenEndpoint = Settings.OAUTHURL;
                var grantType = Settings.GRANTTYPE;
                var scope = Settings.SCOPE;

                using (var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", grantType!),
                        new KeyValuePair<string, string>("scope", scope!)
                    });

                    using (var response = await _httpClient.SendAsync(request))
                    {
                        var result = await response.Content.ReadAsStringAsync();

                        _logger.LogInformation($"Token retrieval completed with a status of {response.StatusCode} - {response.ReasonPhrase}.");

                        token = !string.IsNullOrEmpty(result) || !string.IsNullOrWhiteSpace(result)
                            ? JsonConvert.DeserializeObject<Token?>(result)
                            : null;
                    }
                }

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Token retrieval threw an exception.");
                _logger.LogError(ex.Message, ex);
                throw;
            }
        }
    }
}