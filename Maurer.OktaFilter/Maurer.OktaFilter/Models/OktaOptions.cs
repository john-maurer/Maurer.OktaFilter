using System.ComponentModel.DataAnnotations;

namespace Maurer.OktaFilter.Models
{
    public sealed class OktaOptions : AbstractFilterOptions
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
        /// Grant Type.
        /// </summary>

        [Required] 
        public string GRANT { get; init; } = "";


        /// <summary>
        /// Permissions requested when asking for a token.
        /// </summary>

        [Required] 
        public string SCOPE { get; init; } = "";
    }
}
