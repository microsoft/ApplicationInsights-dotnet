namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.IO;
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
            string configPath = GetConfigFilePath();
            
            if (configPath == null)
            {
                return null;
            }

            try
            {
                // Ensure config file actually exists
                if (!File.Exists(configPath))
                {
                    WebEventSource.Log.ApplicationInsightsConfigNotFound(configPath);
                    return null;
                }

                return ReadConnectionStringFromConfig(configPath);
            }
            catch (FileNotFoundException)
            {
                WebEventSource.Log.ApplicationInsightsConfigNotFound(configPath);
            }
            catch (DirectoryNotFoundException)
            {
                WebEventSource.Log.ApplicationInsightsConfigNotFound(configPath);
            }
            catch (IOException)
            {
                WebEventSource.Log.ApplicationInsightsConfigNotFound(configPath);
            }
            catch (UnauthorizedAccessException)
            {
                WebEventSource.Log.ApplicationInsightsConfigReadError("UnauthorizedAccessException reading config file");
            }
            catch (System.Security.SecurityException)
            {
                WebEventSource.Log.ApplicationInsightsConfigReadError("SecurityException reading config file");
            }
            catch (Exception ex)
            {
                WebEventSource.Log.ApplicationInsightsConfigReadError(ex.ToString());
            }

            return null;
        }

        /// <summary>
        /// Gets the full path to the ApplicationInsights.config file.
        /// </summary>
        /// <returns>The full path to the config file, or null if not found.</returns>
        private static string GetConfigFilePath()
        {
            try
            {
                // Config file should be in the base directory of the app domain
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            }
            catch (System.Security.SecurityException)
            {
                WebEventSource.Log.ApplicationInsightsConfigReadError("SecurityException accessing AppDomain.CurrentDomain.BaseDirectory");
                return null;
            }
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
