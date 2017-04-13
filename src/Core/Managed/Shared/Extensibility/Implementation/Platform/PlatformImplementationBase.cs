namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Security;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Base class implementation of <see cref="IPlatform"/>.
    /// </summary>
    internal abstract class PlatformImplementationBase : IPlatform
    {
        private readonly IDictionary environmentVariables;
        private IDebugOutput debugOutput;
        private string hostName;

        protected PlatformImplementationBase()
        {
            try
            {
                this.environmentVariables = Environment.GetEnvironmentVariables();
            }
            catch (SecurityException e)
            {
                CoreEventSource.Log.FailedToLoadEnvironmentVariables(e.ToString());
            }
        }

        /// <summary>
        /// Gets the directory where the configuration file might be found.
        /// </summary>
        protected abstract string ConfigurationXmlDirectory { get; }

        /// <summary>
        /// Get the platform specific Debugger writer to the VS output console.
        /// </summary>
        /// <returns>The debugger writer.</returns>
        public IDebugOutput GetDebugOutput()
        {
            return this.debugOutput ?? (this.debugOutput = new TelemetryDebugWriter());
        }

        /// <summary>
        /// Get an environment variable value.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable.</returns>
        public string GetEnvironmentVariable(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException();
            }

            object resultObj = this.environmentVariables?[name];
            return resultObj != null ? resultObj.ToString() : null;
        }

        /// <summary>
        /// Returns the machine name.
        /// </summary>
        /// <returns>The machine name.</returns>
        public string GetMachineName()
        {
            return LazyInitializer.EnsureInitialized(ref this.hostName, this.GetHostName);
        }

        /// <summary>
        /// Returns contents of the ApplicationInsights.config file in the application directory.
        /// </summary>
        public string ReadConfigurationXml()
        {
            // Config file should be in the base directory of the app domain
            string configFilePath = Path.Combine(this.ConfigurationXmlDirectory, "ApplicationInsights.config");

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
        /// Gets the host (machine) name.
        /// </summary>
        /// <returns>The host name.</returns>
        protected virtual string GetHostNameCore()
        {
            // This may return null in non-Windows environments.
            return this.GetEnvironmentVariable("COMPUTERNAME");
        }

        /// <summary>
        /// Gets the host name.
        /// </summary>
        /// <returns>The host name.</returns>
        private string GetHostName()
        {
            try
            {
                return this.GetHostNameCore();
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.FailedToGetMachineName(ex.Message);
            }

            return string.Empty;
        }
    }
}
