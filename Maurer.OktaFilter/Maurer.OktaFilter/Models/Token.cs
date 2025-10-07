using Newtonsoft.Json;

namespace Maurer.OktaFilter.Models
{
    public class Token
    {
        public Token()
        {
            AccessToken = string.Empty;
            TokenType = string.Empty;
            ExpiresIn = string.Empty;
            Scope = string.Empty;
        }

        public Token(Token token)
        {
            AccessToken = token.AccessToken;
            TokenType = token.TokenType;
            ExpiresIn = token.ExpiresIn;
            Scope = token.Scope;
        }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public string ExpiresIn { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}