// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsTarget.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.NLogTarget
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using NLog;
    using NLog.Common;
    using NLog.Config;
    using NLog.Targets;

    /// <summary>
    /// NLog Target that routes all logging output to the Application Insights logging framework.
    /// The messages will be uploaded to the Application Insights cloud service.
    /// </summary>
    [Target("ApplicationInsightsTarget")]
    public sealed class ApplicationInsightsTarget : TargetWithContext
    {
        private const string ConnectionStringRequiredMessage = "Azure Monitor connection string is required. Please provide a valid connection string.";

        private TelemetryClient telemetryClient;
        private TelemetryConfiguration telemetryConfiguration;
        private DateTime lastLogEventTime;
        private NLog.Layouts.Layout connectionStringLayout = string.Empty;

        /// <summary>
        /// Initializers a new instance of ApplicationInsightsTarget type.
        /// </summary>
        public ApplicationInsightsTarget()
        {
            this.Layout = @"${message}";
            this.OptimizeBufferReuse = true;
            this.IncludeEventProperties = true;
        }

        /// <summary>
        /// Gets or sets the Application Insights connection string for your application. 
        /// </summary>
        public string ConnectionString
        {
            get => (this.connectionStringLayout as NLog.Layouts.SimpleLayout)?.Text ?? null;
            set => this.connectionStringLayout = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the logging controller we will be using.
        /// </summary>
        internal TelemetryClient TelemetryClient
        {
            get { return this.telemetryClient; }
        }

        internal void BuildPropertyBag(LogEventInfo logEvent, ITelemetry trace)
        {
            trace.Timestamp = logEvent.TimeStamp;

            IDictionary<string, string> propertyBag;

            if (trace is ExceptionTelemetry)
            {
                propertyBag = ((ExceptionTelemetry)trace).Properties;
            }
            else
            {
                propertyBag = ((TraceTelemetry)trace).Properties;
            }

            if (!string.IsNullOrEmpty(logEvent.LoggerName))
            {
                propertyBag.Add("LoggerName", logEvent.LoggerName);
            }

            if (logEvent.UserStackFrame != null)
            {
                propertyBag.Add("UserStackFrame", logEvent.UserStackFrame.ToString());
                propertyBag.Add("UserStackFrameNumber", logEvent.UserStackFrameNumber.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                var callsiteClassName = logEvent.CallerClassName;
                if (!string.IsNullOrEmpty(callsiteClassName))
                    propertyBag.Add("UserStackClassName", callsiteClassName);
                var callsiteMemberName = logEvent.CallerMemberName;
                if (!string.IsNullOrEmpty(callsiteMemberName))
                    propertyBag.Add("UserStackMemberName", callsiteMemberName);
                var callsiteSourceFilePath = logEvent.CallerFilePath;
                if (!string.IsNullOrEmpty(callsiteSourceFilePath))
                    propertyBag.Add("UserStackSourceFile", callsiteSourceFilePath);
                var callsiteSourceLineNumber = logEvent.CallerLineNumber;
                if (callsiteSourceLineNumber != 0)
                    propertyBag.Add("UserStackSourceLine", callsiteSourceLineNumber.ToString());
            }

            if (ShouldIncludeProperties(logEvent) || ContextProperties.Count > 0)
            {
                this.GetAllProperties(logEvent, new StringDictionaryConverter(propertyBag));
            }
        }

        /// <summary>
        /// Initializes the Target and perform connection string validation.
        /// </summary>
        /// <exception cref="NLogConfigurationException">Will throw when <see cref="ConnectionString"/> is not set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();

            string connectionString = this.connectionStringLayout.Render(LogEventInfo.CreateNullEvent());
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new NLogConfigurationException(ConnectionStringRequiredMessage);
            }

            this.telemetryConfiguration = new TelemetryConfiguration();
            this.telemetryConfiguration.ConnectionString = connectionString;
            this.telemetryClient = new TelemetryClient(this.telemetryConfiguration);

            // TODO: Uncomment once SdkVersionUtils is available.
            // this.telemetryClient.Context.GetInternalContext().SdkVersion = SdkVersionUtils.GetSdkVersion("nlog:");
        }

        /// <summary>
        /// Closes the Target and disposes the TelemetryConfiguration.
        /// </summary>
        protected override void CloseTarget()
        {
            base.CloseTarget();
            this.telemetryConfiguration?.Dispose();
            this.telemetryConfiguration = null;
            this.telemetryClient = null;
        }

        /// <summary>
        /// Releases the resources used by the target.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.telemetryConfiguration?.Dispose();
                this.telemetryConfiguration = null;
                this.telemetryClient = null;
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Send the log message to Application Insights.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="logEvent"/> is null.</exception>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException(nameof(logEvent));
            }

            if (this.telemetryClient == null)
            {
                throw new NLogRuntimeException(ConnectionStringRequiredMessage);
            }

            this.lastLogEventTime = DateTime.UtcNow;

            if (logEvent.Exception != null)
            {
                this.SendException(logEvent);
            }
            else
            {
                this.SendTrace(logEvent);
            }
        }

        /// <summary>
        /// Flush any pending log messages.
        /// </summary>
        /// <param name="asyncContinuation">The asynchronous continuation.</param>
        protected override void FlushAsync(AsyncContinuation asyncContinuation)
        {
            if (asyncContinuation == null)
            {
                throw new ArgumentNullException(nameof(asyncContinuation));
            }

            try
            {
                if (this.telemetryClient == null)
                {
                    asyncContinuation(null);
                }
                else
                {
                    this.TelemetryClient.Flush();
                    if (DateTime.UtcNow.AddSeconds(-30) > this.lastLogEventTime)
                    {
                        // Nothing has been written, so nothing to wait for
                        asyncContinuation(null);
                    }
                    else
                    {
                        // Documentation says it is important to wait after flush, else nothing will happen
                        // https://docs.microsoft.com/azure/application-insights/app-insights-api-custom-events-metrics#flushing-data
                        System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(500)).ContinueWith((task) => asyncContinuation(null));
                    }
                }
            }
            catch (Exception ex)
            {
                asyncContinuation(ex);
            }
        }

        private static SeverityLevel? GetSeverityLevel(LogLevel logEventLevel)
        {
            if (logEventLevel == null)
            {
                return null;
            }

            if (logEventLevel.Ordinal == LogLevel.Trace.Ordinal ||
                logEventLevel.Ordinal == LogLevel.Debug.Ordinal)
            {
                return SeverityLevel.Verbose;
            }

            if (logEventLevel.Ordinal == LogLevel.Info.Ordinal)
            {
                return SeverityLevel.Information;
            }

            if (logEventLevel.Ordinal == LogLevel.Warn.Ordinal)
            {
                return SeverityLevel.Warning;
            }

            if (logEventLevel.Ordinal == LogLevel.Error.Ordinal)
            {
                return SeverityLevel.Error;
            }

            if (logEventLevel.Ordinal == LogLevel.Fatal.Ordinal)
            {
                return SeverityLevel.Critical;
            }

            // The only possible value left if OFF but we should never get here in this case
            return null;
        }

        private void SendException(LogEventInfo logEvent)
        {
            if (logEvent.Exception == null)
            {
                InternalLogger.Warn("SendException called without an exception instance.");
                return;
            }

            var exceptionTelemetry = new ExceptionTelemetry(logEvent.Exception)
            {
                SeverityLevel = GetSeverityLevel(logEvent.Level),
                Message = $"{logEvent.Exception.GetType().ToString()}: {logEvent.Exception.Message}",
            };

            string logMessage = this.RenderLogEvent(this.Layout, logEvent);
            if (!string.IsNullOrEmpty(logMessage))
            {
                exceptionTelemetry.Properties.Add("Message", logMessage);
            }

            InternalLogger.Debug("ApplicationInsightsTarget.SendException building property bag.");
            this.BuildPropertyBag(logEvent, exceptionTelemetry);
            InternalLogger.Debug("ApplicationInsightsTarget.SendException tracking exception telemetry.");
            this.telemetryClient.TrackException(exceptionTelemetry);
            InternalLogger.Debug("ApplicationInsightsTarget.SendException completed.");
        }

        private void SendTrace(LogEventInfo logEvent)
        {
            string logMessage = this.RenderLogEvent(this.Layout, logEvent);
            var trace = new TraceTelemetry(logMessage)
            {
                SeverityLevel = GetSeverityLevel(logEvent.Level),
            };

            this.BuildPropertyBag(logEvent, trace);
            this.telemetryClient.TrackTrace(trace);
        }
    }
}