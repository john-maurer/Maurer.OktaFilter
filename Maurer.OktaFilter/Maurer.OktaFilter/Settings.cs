using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

namespace Maurer.OktaFilter
{
    static public class Settings
    {
        private static bool _validated = false;

        public static void Validate()
        {
            if (!_validated)
            {

                foreach (var property in typeof(Settings).GetType().GetProperties(BindingFlags.Static | BindingFlags.Public))
                {
                    if (string.IsNullOrEmpty(property.GetValue(null) as string))
                        throw new InvalidOperationException($"{property.Name} must be provided.");
                }

                _validated = true;
            }
        }

        public static string OAUTHUSER { get; set; } = string.Empty;

        public static string OAUTHPASSWORD { get; set; } = string.Empty;

        public static string OAUTHURL { get; set; } = string.Empty;

        public static string OAUTHKEY { get; set; } = string.Empty;

        public static string GRANTTYPE { get; set; } = string.Empty;

        public static string SCOPE { get; set; } = string.Empty;

        public static string RETRIES { get; set; } = string.Empty;

        public static string RETRYSLEEP { get; set; } = string.Empty;

        public static string TOKENLIFETIME { get; set; } = string.Empty;
    }
}