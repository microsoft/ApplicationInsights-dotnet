#if !NETSTANDARD1_3 // netstandard1.3 has it's own implementation
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Security;
    using System.Threading;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    
    /// <summary>
    /// The .NET 4.0 and 4.5 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal class PlatformImplementation : IPlatform
    {
        private readonly IDictionary environmentVariables;

        private IDebugOutput debugOutput = null;
        private string hostName;

        /// <summary>
        /// Initializes a new instance of the PlatformImplementation class.
        /// </summary>
        public PlatformImplementation()
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
                throw new ArgumentNullException(nameof(name));
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

        private string GetHostName()
        {
            try
            {
                string domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                string hostName = Dns.GetHostName();

                if (!hostName.EndsWith(domainName, StringComparison.OrdinalIgnoreCase))
                {
                    hostName = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", hostName, domainName);
                }

                return hostName;
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.FailedToGetMachineName(ex.Message);
            }

            return string.Empty;
        }
    }
}
#else
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    internal class PlatformImplementation : IPlatform
    {
        private IDebugOutput debugOutput = null;
        
        public IDictionary<string, object> GetApplicationSettings()
        {
            return null;
        }

        public string ReadConfigurationXml()
        {
            return null;
        }

        public ExceptionDetails GetExceptionDetails(Exception exception, ExceptionDetails parentExceptionDetails)
        {
            return ExceptionConverter.ConvertToExceptionDetails(exception, parentExceptionDetails);
        }

        /// <summary>
        /// Returns the platform specific Debugger writer to the VS output console.
        /// </summary>
        public IDebugOutput GetDebugOutput()
        {
            if (this.debugOutput == null)
            {
                this.debugOutput = new TelemetryDebugWriter(); 
            }
            
            return this.debugOutput;
        }

        public string GetEnvironmentVariable(string name)
        {
            return Environment.GetEnvironmentVariable(name);
        }

        /// <summary>
        /// Returns the machine name.
        /// </summary>
        /// <returns>The machine name.</returns>
        public string GetMachineName()
        {
            return Environment.GetEnvironmentVariable("COMPUTERNAME");
        }
    }
}
#endif