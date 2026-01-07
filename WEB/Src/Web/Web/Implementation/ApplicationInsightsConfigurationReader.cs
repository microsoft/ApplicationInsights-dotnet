namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml.Linq;

    /// <summary>
    /// Reads configuration from ApplicationInsights.config file.
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
            var options = GetConfigurationOptions();
            return options?.ConnectionString;
        }

        /// <summary>
        /// Reads all configuration options from ApplicationInsights.config file.
        /// </summary>
        /// <returns>The configuration options if found; otherwise, null.</returns>
        public static ApplicationInsightsConfigOptions GetConfigurationOptions()
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

                return ReadConfigurationFromFile(configPath);
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
        /// <returns>The configuration options if found; otherwise, null.</returns>
        private static ApplicationInsightsConfigOptions ReadConfigurationFromFile(string configPath)
        {
            XDocument xml = XDocument.Load(configPath);
            XElement root = xml.Element(XmlNamespace + "ApplicationInsights");
            
            if (root == null)
            {
                WebEventSource.Log.ApplicationInsightsConfigConnectionStringNotFound(configPath);
                return null;
            }

            var options = new ApplicationInsightsConfigOptions();
            bool hasAnyValue = false;

            // Read ConnectionString
            string connectionString = ReadStringElement(root, "ConnectionString");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                options.ConnectionString = connectionString;
                hasAnyValue = true;
                WebEventSource.Log.ApplicationInsightsConfigLoaded(configPath);
            }
            else
            {
                WebEventSource.Log.ApplicationInsightsConfigConnectionStringNotFound(configPath);
            }

            // Read Core Settings
            options.DisableTelemetry = ReadBoolElement(root, "DisableTelemetry");
            options.ApplicationVersion = ReadStringElement(root, "ApplicationVersion");

            // Read Sampling Settings
            options.SamplingRatio = ReadFloatElement(root, "SamplingRatio");
            options.TracesPerSecond = ReadDoubleElement(root, "TracesPerSecond");

            // Read Storage Settings
            options.StorageDirectory = ReadStringElement(root, "StorageDirectory");
            options.DisableOfflineStorage = ReadBoolElement(root, "DisableOfflineStorage");

            // Read Feature Flags
            options.EnableQuickPulseMetricStream = ReadBoolElement(root, "EnableQuickPulseMetricStream");
            options.EnableTraceBasedLogsSampler = ReadBoolElement(root, "EnableTraceBasedLogsSampler");
            options.EnablePerformanceCounterCollectionModule = ReadBoolElement(root, "EnablePerformanceCounterCollectionModule");
            options.AddAutoCollectedMetricExtractor = ReadBoolElement(root, "AddAutoCollectedMetricExtractor");
            options.EnableDependencyTrackingTelemetryModule = ReadBoolElement(root, "EnableDependencyTrackingTelemetryModule");

            // Track if any value was set
            if (options.DisableTelemetry.HasValue || options.SamplingRatio.HasValue || 
                options.TracesPerSecond.HasValue || options.DisableOfflineStorage.HasValue ||
                options.EnableQuickPulseMetricStream.HasValue || options.EnableTraceBasedLogsSampler.HasValue ||
                options.EnablePerformanceCounterCollectionModule.HasValue || options.AddAutoCollectedMetricExtractor.HasValue ||
                options.EnableDependencyTrackingTelemetryModule.HasValue || !string.IsNullOrWhiteSpace(options.StorageDirectory) ||
                !string.IsNullOrWhiteSpace(options.ApplicationVersion))
            {
                hasAnyValue = true;
            }

            return hasAnyValue ? options : null;
        }

        /// <summary>
        /// Reads a string element from the XML.
        /// </summary>
        private static string ReadStringElement(XElement root, string elementName)
        {
            XElement element = root.Element(XmlNamespace + elementName);
            if (element != null && !string.IsNullOrWhiteSpace(element.Value))
            {
                return element.Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// Reads a boolean element from the XML.
        /// </summary>
        private static bool? ReadBoolElement(XElement root, string elementName)
        {
            XElement element = root.Element(XmlNamespace + elementName);
            if (element != null && !string.IsNullOrWhiteSpace(element.Value))
            {
                if (bool.TryParse(element.Value.Trim(), out bool result))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads a float element from the XML.
        /// </summary>
        private static float? ReadFloatElement(XElement root, string elementName)
        {
            XElement element = root.Element(XmlNamespace + elementName);
            if (element != null && !string.IsNullOrWhiteSpace(element.Value))
            {
                if (float.TryParse(element.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Reads a double element from the XML.
        /// </summary>
        private static double? ReadDoubleElement(XElement root, string elementName)
        {
            XElement element = root.Element(XmlNamespace + elementName);
            if (element != null && !string.IsNullOrWhiteSpace(element.Value))
            {
                if (double.TryParse(element.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
            }

            return null;
        }
    }
}
