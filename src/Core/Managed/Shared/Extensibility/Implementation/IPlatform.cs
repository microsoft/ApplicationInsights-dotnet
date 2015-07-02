namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Encapsulates platform-specific functionality required by the API.
    /// </summary>
    /// <remarks>
    /// This type is public to enable mocking on Windows Phone.
    /// </remarks>
    internal interface IPlatform
    {
        /// <summary>
        /// Returns a dictionary that can be used to access per-user/per-application settings shared by all application instances.
        /// </summary>
        IDictionary<string, object> GetApplicationSettings();

        /// <summary>
        /// Returns contents of the ApplicationInsights.config file in the application directory.
        /// </summary>
        string ReadConfigurationXml();

        /// <summary>
        /// Returns the platform specific <see cref="ExceptionDetails"/> object for the given Exception.
        /// </summary>
        ExceptionDetails GetExceptionDetails(Exception exception, ExceptionDetails parentExceptionDetails);

        /// <summary>
        /// Returns the platform specific Debugger writer to the VS output console.
        /// </summary>
        IDebugOutput GetDebugOutput();
    }
}
