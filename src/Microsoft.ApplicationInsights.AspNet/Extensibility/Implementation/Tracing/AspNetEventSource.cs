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
        /// <summary>
        /// The singleton instance of this event source.
        /// Due to how EventSource initialization works this has to be a public field and not
        /// a property otherwise the internal state of the event source will not be enabled.
        /// </summary>
        public static readonly AspNetEventSource Instance = new AspNetEventSource();

        /// <summary>
        /// Prevents a default instance of the AspNetEventSource class from being created.
        /// </summary>
        private AspNetEventSource() : base()
        {
            try
            {
                this.ApplicationName = Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName;
            }
            catch (Exception exp)
            {
                this.ApplicationName = "Undefined " + exp.Message;
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
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(6, Message = "{0}", Level = EventLevel.LogAlways, Keywords = Keywords.Diagnostics)]
        public void LogAlways(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the critical message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event((int)EventLevel.Critical, Message = "{0}", Level = EventLevel.Critical, Keywords = Keywords.Diagnostics)]
        public void LogCritical(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent((int)EventLevel.Critical, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the error message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event((int)EventLevel.Error, Message = "{0}", Level = EventLevel.Error, Keywords = Keywords.Diagnostics)]
        public void LogError(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent((int)EventLevel.Error, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the warning message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event((int)EventLevel.Warning, Message = "{0}", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogWarning(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent((int)EventLevel.Warning, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the informational message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event((int)EventLevel.Informational, Message = "{0}", Level = EventLevel.Informational, Keywords = Keywords.Diagnostics)]
        public void LogInformational(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent((int)EventLevel.Informational, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event for the verbose message level.
        /// </summary>
        /// <param name="message">The message to write an event for.</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event((int)EventLevel.Verbose, Message = "{0}", Level = EventLevel.Verbose, Keywords = Keywords.Diagnostics)]
        public void LogVerbose(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent((int)EventLevel.Verbose, message ?? string.Empty, this.ApplicationName);
        }

        /// <summary>
        /// Keywords for the AspNetEventSource.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Keyword for errors that trace at Verbose level.
            /// </summary>
            public const EventKeywords Diagnostics = (EventKeywords)0x1;
        }
    }
}
