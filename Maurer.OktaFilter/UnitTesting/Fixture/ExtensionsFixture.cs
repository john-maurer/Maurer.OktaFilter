using Microsoft.Extensions.Configuration;

namespace UnitTesting.Fixture
{
    public class ExtensionsFixture : AbstractFixture
    {
        protected override void Arrange(params object[] parameters)
        {
            HttpsConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Okta:USER"] = "client_id",
                ["Okta:PASSWORD"] = "secret",
                ["Okta:AUTHURL"] = "https://secure.com/oauth2/v1/token",
                ["Okta:AUTHKEY"] = "OKTA-TOKEN",
                ["Okta:GRANT"] = "client_credentials",
                ["Okta:SCOPE"] = "openid",
                ["Okta:RETRIES"] = "1",
                ["Okta:SLEEP"] = "0",
                ["Okta:LIFETIME"] = "5",
            }).Build();

            HttpConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Okta:USER"] = "client_id",
                ["Okta:PASSWORD"] = "secret",
                ["Okta:AUTHURL"] = "http://insecure.local/token", // not HTTPS → should fail
                ["Okta:AUTHKEY"] = "OKTA-TOKEN",
                ["Okta:GRANT"] = "client_credentials",
                ["Okta:SCOPE"] = "openid",
                ["Okta:RETRIES"] = "1",
                ["Okta:SLEEP"] = "0",
                ["Okta:LIFETIME"] = "5",
            }).Build();
        }

        public IConfiguration HttpsConfiguration { get; set; }

        public IConfiguration HttpConfiguration { get; set; }
    }
}
