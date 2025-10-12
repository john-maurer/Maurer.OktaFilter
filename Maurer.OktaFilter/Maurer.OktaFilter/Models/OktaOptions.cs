using System.ComponentModel.DataAnnotations;

namespace Maurer.OktaFilter.Models
{
    public sealed class OktaOptions
    {
        /// <summary>
        /// OKTA user name.
        /// </summary>

        [Required] 
        public string USER { get; init; } = "";

        /// <summary>
        /// OKTA password.
        /// </summary>

        [Required] 
        public string PASSWORD { get; init; } = "";

        /// <summary>
        /// OTKA OAuth url (absolute HTTPS URL).
        /// </summary>

        [Required] 
        public string OAUTHURL { get; init; } = "";      

        /// <summary>
        /// Cache key used in DistributedCache to store the OKTA token.
        /// </summary>

        [Required] 
        public string OAUTHKEY { get; init; } = "";

        /// <summary>
        /// Grant Type.
        /// </summary>

        [Required] 
        public string GRANT { get; init; } = "";


        /// <summary>
        /// Permissions requested when asking for a token.
        /// </summary>

        [Required] 
        public string SCOPE { get; init; } = "";

        /// <summary>
        /// Number of times to retry (max 10).
        /// </summary>

        [Range(0, 10)] 
        public int RETRIES { get; init; } = 2;

        /// <summary>
        /// Period of time to sleep in seconds (max 300).
        /// </summary>

        [Range(0, 300)] 
        public int SLEEP { get; init; } = 1;

        /// <summary>
        /// Token lifetime in minutes (max 1440).
        /// </summary>

        [Range(0, 1440)] 
        public int LIFETIME { get; init; } = 30;
    }
}
