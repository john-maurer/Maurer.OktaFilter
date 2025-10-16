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
        private readonly OktaOptions _options;

        private OktaToken? ParseToken(string responseBody)
        {
            try
            {
                return JsonConvert.DeserializeObject<OktaToken?>(responseBody);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public TokenService(HttpClient httpClient, OktaOptions options, ILogger<TokenService> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<OktaToken?> GetToken(CancellationToken cancellationToken = default)
        {
            try
            {
                //Settings.Validate();

                var credentials = Convert.ToBase64String
                    (Encoding.UTF8.GetBytes($"{_options.USER}:{_options.PASSWORD}"));

                //Restrict to HTTPS - Prevents accidental plaintext or SSRF to non-HTTPS
                if (!Uri.TryCreate(_options.OAUTHURL, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    throw new InvalidOperationException("OAUTHURL must be an absolute HTTPS URL.");

                //Create request that prefers HTTP/2; fail fast on non-2xx; don't buffer unnecessarily.
                using (var request = new HttpRequestMessage(HttpMethod.Post, uri) { Version = HttpVersion.Version20, VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher })
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    request.Content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", _options.GRANT!),
                        new KeyValuePair<string, string>("scope", _options.SCOPE!)
                    });

                    //Create response that prefers non-buffering completion and status validation before reading body
                    using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                    {
                        _logger.LogInformation("Token retrieval completed with a status of '{Status} - {Reason}'.", response.StatusCode, response.ReasonPhrase);

                        var result = await response.Content.ReadAsStringAsync(cancellationToken);

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