namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Security;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// The .NET 4.0 and 4.5 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal class PlatformImplementation : IPlatform
    {
        private readonly IDictionary environmentVariables = Environment.GetEnvironmentVariables();

        private IDebugOutput debugOutput = null;

        /// <summary>
        /// Returns contents of the ApplicationInsights.config file in the application directory.
        /// </summary>
        public string ReadConfigurationXml()
        {
            // Config file should be in the base directory of the app domain
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");

            try
            {
                // Ensure config file actually exists
                if (File.Exists(configFilePath))
                {
                    return File.ReadAllText(configFilePath);
                }
            }
            catch (FileNotFoundException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (DirectoryNotFoundException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (IOException)
            {
                // For cases when file was deleted/modified while reading
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (UnauthorizedAccessException)
            {
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }
            catch (SecurityException)
            {
                CoreEventSource.Log.ApplicationInsightsConfigNotFoundWarning(configFilePath);
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns the platform specific Debugger writer to the VS output console.
        /// </summary>
        public IDebugOutput GetDebugOutput()
        {
            return this.debugOutput ?? (this.debugOutput = new TelemetryDebugWriter());
        }

        public string GetEnvironmentVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException();
            }

            object resultObj = this.environmentVariables[name];
            return resultObj != null ? resultObj.ToString() : null;
        }
    }
}
