using Newtonsoft.Json;
using System.Text;

namespace IntegrationTesting.Utilities
{
    public sealed class StubbedTokenHandler : DelegatingHandler
    {
        private readonly Uri _endpoint;
        private readonly Queue<string> _payloads;
        private int _callCount;

        public int CallCount => _callCount;

        public StubbedTokenHandler(Uri endpoint, params string[] jsonPayloads)
        {
            _endpoint = endpoint;
            _payloads = new Queue<string>(jsonPayloads.Length == 0
                ? new[] { JsonConvert.SerializeObject(new { access_token = "stub-token", token_type = "Bearer", expires_in = 300, scope = "openid" }) }
                : jsonPayloads);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri == _endpoint && request.Method == HttpMethod.Post)
            {
                Interlocked.Increment(ref _callCount);
                var body = _payloads.Count > 0 ? _payloads.Dequeue() : "{\"access_token\":\"stub-token\",\"token_type\":\"Bearer\",\"expires_in\":300,\"scope\":\"openid\"}";
                var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        }
    }
}
