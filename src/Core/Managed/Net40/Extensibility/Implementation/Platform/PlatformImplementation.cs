namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// The .NET 4.0 and 4.5 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal class PlatformImplementation : 
        IPlatform
    {
        public IDictionary<string, object> GetApplicationSettings()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns contents of the ApplicationInsights.config file in the application directory.
        /// </summary>
        public string ReadConfigurationXml()
        {
            // Config file should be in the base directory of the app domain
            string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ApplicationInsights.config");

            // Ensure config file actually exists
            if (File.Exists(configFilePath))
            {
                return File.ReadAllText(configFilePath);
            }

            return string.Empty;
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
            return new TelemetryDebugWriter();
        }
    }
}
