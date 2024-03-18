namespace UnitTesting
{
    /// <summary>
    /// Represents XUnit Testing Harness Settings
    /// </summary>

    static public class Settings
    {
        /// <summary>
        /// Static default constructor
        /// </summary>

        static Settings()
        {
            Environment = "Development";
        }

        /// <summary>
        /// Represents pre-defined testing environment
        /// </summary>

        static public string Environment { get; set; }

        /// <summary>
        /// Relative path to the application configuration JSON file
        /// </summary>

        static public string AppConfiguration { get; set; }
    }
}
