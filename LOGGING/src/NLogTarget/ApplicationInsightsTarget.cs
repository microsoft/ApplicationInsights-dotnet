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
    public sealed class ApplicationInsightsTarget : TargetWithLayout
    {
        private const string ConnectionStringRequiredMessage = "Azure Monitor connection string is required. Please provide a valid connection string.";

        private TelemetryClient telemetryClient;
        private TelemetryConfiguration telemetryConfiguration;
        private DateTime lastLogEventTime;
        private NLog.Layouts.Layout connectionStringLayout = string.Empty;
        private bool initializationFailed;

        /// <summary>
        /// Initializers a new instance of ApplicationInsightsTarget type.
        /// </summary>
        public ApplicationInsightsTarget()
        {
            this.Layout = @"${message}";
            this.OptimizeBufferReuse = true;
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
        /// Gets the array of custom attributes to be passed into the logevent context.
        /// </summary>
        [ArrayParameter(typeof(TargetPropertyWithContext), "contextproperty")]
        public IList<TargetPropertyWithContext> ContextProperties { get; } = new List<TargetPropertyWithContext>();

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
            trace.Sequence = logEvent.SequenceID.ToString(CultureInfo.InvariantCulture);

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

            for (int i = 0; i < this.ContextProperties.Count; ++i)
            {
                var contextProperty = this.ContextProperties[i];
                if (!string.IsNullOrEmpty(contextProperty.Name) && contextProperty.Layout != null)
                {
                    string propertyValue = this.RenderLogEvent(contextProperty.Layout, logEvent);
                    PopulatePropertyBag(propertyBag, contextProperty.Name, propertyValue);
                }
            }

            if (logEvent.HasProperties)
            {
                LoadLogEventProperties(logEvent, propertyBag);
            }
        }

        /// <summary>
        /// Initializes the Target and perform connection string validation.
        /// </summary>
        /// <exception cref="NLogConfigurationException">Will throw when <see cref="ConnectionString"/> is not set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            this.telemetryConfiguration = new TelemetryConfiguration();
            this.initializationFailed = false;

            string connectionString = this.connectionStringLayout.Render(LogEventInfo.CreateNullEvent());
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                this.initializationFailed = true;
                return;
            }

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
            this.initializationFailed = false;
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

            if (this.telemetryClient == null || this.initializationFailed)
            {
                throw new NLogConfigurationException(ConnectionStringRequiredMessage);
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
                if (this.telemetryClient == null || this.initializationFailed)
                {
                    InternalLogger.Debug("ApplicationInsightsTarget.FlushAsync skipped - telemetry client not initialized.");
                    asyncContinuation(null);
                    return;
                }

                InternalLogger.Debug("ApplicationInsightsTarget.FlushAsync flushing telemetry client.");
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
            catch (Exception ex)
            {
                asyncContinuation(ex);
            }
        }

        private static void LoadLogEventProperties(LogEventInfo logEvent, IDictionary<string, string> propertyBag)
        {
            if (logEvent.Properties?.Count > 0)
            {
                foreach (var keyValuePair in logEvent.Properties)
                {
                    string key = keyValuePair.Key.ToString();
                    object valueObj = keyValuePair.Value;
                    PopulatePropertyBag(propertyBag, key, valueObj);
                }
            }
        }

        private static void PopulatePropertyBag(IDictionary<string, string> propertyBag, string key, object valueObj)
        {
            if (valueObj == null)
            {
                return;
            }

            string value = Convert.ToString(valueObj, CultureInfo.InvariantCulture);
            if (propertyBag.ContainsKey(key))
            {
                if (string.Equals(value, propertyBag[key], StringComparison.Ordinal))
                {
                    return;
                }

                key += "_1";
            }

            propertyBag.Add(key, value);
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