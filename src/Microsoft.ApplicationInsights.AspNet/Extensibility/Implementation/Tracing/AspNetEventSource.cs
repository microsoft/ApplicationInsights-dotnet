//-----------------------------------------------------------------------
// <copyright file="AspNetEventSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
namespace Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event source for Application Insights ASP.NET 5 SDK.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-AspNet")]
    internal sealed class AspNetEventSource : EventSource
    {
        /// <summary>Singleton instance variable.</summary>
        private static readonly AspNetEventSource SingletonInstance = new AspNetEventSource();

        /// <summary>
        /// Prevents a default instance of the AspNetEventSource class from being created.
        /// </summary>
        private AspNetEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        /// <summary>
        /// Gets the instance of this event source.
        /// </summary>
        public static AspNetEventSource Instance
        {
            get
            {
                return SingletonInstance;
            }
        }

        /// <summary>
        /// Gets the application name for use in logging events.
        /// </summary>
        public string ApplicationName { [NonEvent] get; [NonEvent]private set; }

        /// <summary>
        /// Logs an event for the always message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.LogAlways, Message = "{0}", Level = EventLevel.LogAlways)]
        public void LogAlways(string message)
        {
            this.WriteEvent((int)EventLevel.LogAlways, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the critical message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.Critical, Message = "{0}", Level = EventLevel.Critical)]
        public void LogCritical(string message)
        {
            this.WriteEvent((int)EventLevel.Critical, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the error message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.Error, Message = "{0}", Level = EventLevel.Error)]
        public void LogError(string message)
        {
            this.WriteEvent((int)EventLevel.Error, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the warning message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.Warning, Message = "{0}", Level = EventLevel.Warning)]
        public void LogWarning(string message)
        {
            this.WriteEvent((int)EventLevel.Warning, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the informational message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.Informational, Message = "{0}", Level = EventLevel.Informational)]
        public void LogInformational(string message)
        {
            this.WriteEvent((int)EventLevel.Informational, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the verbose message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">The name of the application domain.</param>
        [Event((int)EventLevel.Verbose, Message = "{0}", Level = EventLevel.Verbose)]
        public void LogVerbose(string message)
        {
            this.WriteEvent((int)EventLevel.Verbose, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Gets the friendly name of the current application domain if possible otherwise
        /// it is equal to undefined and an exception message.
        /// </summary>
        /// <returns>The application name.</returns>
        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
                name = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName;
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp.Message;
            }

            return name;
        }
    }
}
