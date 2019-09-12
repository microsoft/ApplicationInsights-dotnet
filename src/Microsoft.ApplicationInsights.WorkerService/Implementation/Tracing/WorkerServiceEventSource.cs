//-----------------------------------------------------------------------
// <copyright file="WorkerServiceEventSource.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.WorkerService.Implementation.Tracing
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event source for Application Insights Worker Service SDK.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-WorkerService")]
    [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification = "appDomainName is required")]
    internal sealed class WorkerServiceEventSource : EventSource
    {
        /// <summary>
        /// The singleton instance of this event source.
        /// Due to how EventSource initialization works this has to be a public field and not
        /// a property otherwise the internal state of the event source will not be enabled.
        /// </summary>
        public static readonly WorkerServiceEventSource Instance = new WorkerServiceEventSource();

        /// <summary>
        /// Prevents a default instance of the <see cref="WorkerServiceEventSource"/> class from being created.
        /// </summary>
        private WorkerServiceEventSource()
            : base()
        {
            try
            {
                this.ApplicationName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            }
            catch (Exception exp)
            {
                this.ApplicationName = "Undefined " + exp.Message;
            }
        }

        /// <summary>
        /// Gets the application name for use in logging events.
        /// </summary>
        public string ApplicationName
        {
            [NonEvent]
            get;
            [NonEvent]
            private set;
        }

        /// <summary>
        /// Logs informational message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(1, Message = "Message : {0}", Level = EventLevel.Warning, Keywords = Keywords.Diagnostics)]
        public void LogInformational(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, message, this.ApplicationName);
        }

        /// <summary>
        /// Logs warning message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(2, Message = "Message : {0}", Level = EventLevel.Warning)]
        public void LogWarning(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, message, this.ApplicationName);
        }

        /// <summary>
        /// Logs error message.
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="appDomainName">An ignored placeholder to make EventSource happy.</param>
        [Event(3, Message = "An error has occured which may prevent application insights from functioning. Error message: '{0}'", Level = EventLevel.Error)]
        public void LogError(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, message, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event when a TelemetryModule is not found to configure.
        /// </summary>
        [Event(4, Message = "Unable to configure module {0} as it is not found in service collection.", Level = EventLevel.Error, Keywords = Keywords.Diagnostics)]
        public void UnableToFindModuleToConfigure(string moduleType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, moduleType, this.ApplicationName);
        }

        /// <summary>
        /// Logs an event when TelemetryConfiguration configure has failed.
        /// </summary>
        [Event(
           5,
            Keywords = Keywords.Diagnostics,
            Message = "An error has occured while setting up TelemetryConfiguration. Error message: '{0}' ",
            Level = EventLevel.Error)]
        public void TelemetryConfigurationSetupFailure(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, errorMessage, this.ApplicationName);
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
