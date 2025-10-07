using System.Net.Http.Headers;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Text;
using Maurer.OktaFilter.Interfaces;
using Maurer.OktaFilter.Models;
using System.Net;

namespace Maurer.OktaFilter.Services
{
    public class TokenService : ITokenService
    {
        private readonly ILogger<TokenService> _logger;
        private readonly HttpClient _httpClient;

        private Token? ParseToken(string responseBody)
        {
            try
            {
                return JsonConvert.DeserializeObject<Token?>(responseBody);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public TokenService(ILogger<TokenService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<Token?> GetToken()
        {
            try
            {
                Settings.Validate();

                var credentials = Convert.ToBase64String
                    (Encoding.UTF8.GetBytes($"{Settings.OAUTHUSER}:{Settings.OAUTHPASSWORD}"));

                //Restrict to HTTPS - Prevents accidental plaintext or SSRF to non-HTTPS
                if (!Uri.TryCreate(Settings.OAUTHURL, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    throw new InvalidOperationException("OAUTHURL must be an absolute HTTPS URL.");

                //Create request that prefers HTTP/2; fail fast on non-2xx; don't buffer unnecessarily.
                using (var request = new HttpRequestMessage(HttpMethod.Post, uri) { Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher })
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    request.Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", Settings.GRANTTYPE!),
                        new KeyValuePair<string, string>("scope", Settings.SCOPE!)
                    });

                    //Create response that prefers non-buffering completion and status validation before reading body
                    using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        _logger.LogInformation("Token retrieval completed with a status of '{Status} - {Reason}'.", response.StatusCode, response.ReasonPhrase);

                        var result = await response.Content.ReadAsStringAsync();

                        return !string.IsNullOrWhiteSpace(result) ? ParseToken(result) : null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Token retrieval threw an exception.");
                _logger.LogError(ex, "Error during token retrieval.");
                throw;
            }
        }
    }
}