using System.ComponentModel.DataAnnotations;

namespace Maurer.OktaFilter.Models
{
    public abstract class AbstractFilterOptions
    {
        /// <summary>
        /// Cache key used in DistributedCache to store the OKTA token.
        /// </summary>

        [Required]
        public string AUTHKEY { get; init; } = "";

        /// <summary>
        /// URL to exchange credentials with for a security token (absolute HTTPS URL).
        /// </summary>

        [Required]
        public string AUTHURL { get; init; } = "";

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
