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

        [JsonProperty("AccessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("TokenType")]
        public string TokenType { get; set; }

        [JsonProperty("ExpiresIn")]
        public string ExpiresIn { get; set; }

        [JsonProperty("Scope")]
        public string Scope { get; set; }
    }
}
