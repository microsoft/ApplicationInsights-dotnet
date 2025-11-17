namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Xml.Linq;

    /// <summary>
    /// Reads connection string from ApplicationInsights.config file.
    /// </summary>
    internal static class ApplicationInsightsConfigurationReader
    {
        private const string ConfigFileName = "ApplicationInsights.config";
        private static readonly XNamespace XmlNamespace = "http://schemas.microsoft.com/ApplicationInsights/2013/Settings";

        /// <summary>
        /// Reads the connection string from ApplicationInsights.config file.
        /// </summary>
        /// <returns>The connection string if found; otherwise, null.</returns>
        public static string GetConnectionString()
        {
            try
            {
                string configPath = GetConfigFilePath();
                if (configPath == null || !File.Exists(configPath))
                {
                    WebEventSource.Log.ApplicationInsightsConfigNotFound(configPath ?? "null");
                    return null;
                }

                return ReadConnectionStringFromConfig(configPath);
            }
            catch (Exception ex)
            {
                WebEventSource.Log.ApplicationInsightsConfigReadError(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the full path to the ApplicationInsights.config file.
        /// </summary>
        /// <returns>The full path to the config file, or null if not found.</returns>
        private static string GetConfigFilePath()
        {
            // Try to get the application's base directory
            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                string basePath = HttpContext.Current.Server.MapPath("~");
                if (!string.IsNullOrEmpty(basePath))
                {
                    return Path.Combine(basePath, ConfigFileName);
                }
            }

            // Fallback to AppDomain base directory
            string appDomainPath = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(appDomainPath))
            {
                return Path.Combine(appDomainPath, ConfigFileName);
            }

            return null;
        }

        /// <summary>
        /// Reads the connection string from the XML configuration file.
        /// </summary>
        /// <param name="configPath">The path to the config file.</param>
        /// <returns>The connection string if found; otherwise, null.</returns>
        private static string ReadConnectionStringFromConfig(string configPath)
        {
            XDocument xml = XDocument.Load(configPath);
            XElement root = xml.Element(XmlNamespace + "ApplicationInsights");
            
            if (root != null)
            {
                XElement connectionStringElement = root.Element(XmlNamespace + "ConnectionString");
                
                if (connectionStringElement != null && !string.IsNullOrWhiteSpace(connectionStringElement.Value))
                {
                    string connectionString = connectionStringElement.Value.Trim();
                    WebEventSource.Log.ApplicationInsightsConfigLoaded(configPath);
                    return connectionString;
                }
            }

            WebEventSource.Log.ApplicationInsightsConfigConnectionStringNotFound(configPath);
            return null;
        }
    }
}
