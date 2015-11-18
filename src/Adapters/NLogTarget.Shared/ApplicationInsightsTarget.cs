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
    using NLog;
    using NLog.Targets;
      
    /// <summary>
    /// NLog Target that routes all logging output to the Application Insights logging framework.
    /// The messages will be uploaded to the Application Insights cloud service.
    /// </summary>
    [Target("ApplicationInsightsTarget")]
    public sealed class ApplicationInsightsTarget : Target
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// The Application Insights instrumentationKey for your application. 
        /// </summary>
        public string InstrumentationKey { get; set; }

        /// <summary>
        /// The logging controller we will be using.
        /// </summary>
        internal TelemetryClient TelemetryClient
        {
            get { return this.telemetryClient; }
        }

        /// <summary>
        /// Initializes the Target and perform instrumentationKey validation.
        /// </summary>
        /// <exception cref="NLogConfigurationException">Will throw when <see cref="InstrumentationKey"/> is not set.</exception>
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            this.telemetryClient = new TelemetryClient();
            if (!string.IsNullOrEmpty(this.InstrumentationKey))
            {
                this.telemetryClient.Context.InstrumentationKey = this.InstrumentationKey;
            }
        }

        /// <summary>
        /// Send the log message to Application Insights.
        /// </summary>
        /// <exception cref="ArgumentNullException">If <paramref name="logEvent"/> is null.</exception>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }

            if (logEvent.Exception != null)
            {
                this.SendException(logEvent);
            }
            else
            {
                this.SendTrace(logEvent);
            }
        }

        private void SendException(LogEventInfo logEvent)
        {
            var exceptionTelemetry = new ExceptionTelemetry(logEvent.Exception)
            {
                SeverityLevel = this.GetSeverityLevel(logEvent.Level)
            };

            this.BuildPropertyBag(logEvent, exceptionTelemetry);
            this.telemetryClient.Track(exceptionTelemetry);
        }

        private void SendTrace(LogEventInfo logEvent)
        {
            var trace = new TraceTelemetry(logEvent.FormattedMessage)
            {
                SeverityLevel = this.GetSeverityLevel(logEvent.Level)
            };

            this.BuildPropertyBag(logEvent, trace);
            this.telemetryClient.Track(trace);
        }

        private void BuildPropertyBag(LogEventInfo logEvent, ITelemetry trace)
        {
            IDictionary<string, string> propertyBag;

            if (trace is ExceptionTelemetry)
            {
                propertyBag = ((ExceptionTelemetry)trace).Properties;
            }
            else
            {
                propertyBag = ((TraceTelemetry)trace).Properties;
            }

            if (logEvent.Level != null)
            {
                // Log verbosity level
                propertyBag.Add("Level", logEvent.Level.Name);
            }

            propertyBag.Add("SequenceID", logEvent.SequenceID.ToString(CultureInfo.InvariantCulture));
            propertyBag.Add("TimeStamp", logEvent.TimeStamp.ToString(CultureInfo.InvariantCulture));
            propertyBag.Add("SourceType", "NLog");

            if (!string.IsNullOrEmpty(logEvent.LoggerName))
            {
                propertyBag.Add("LoggerName", logEvent.LoggerName);
            }

            if (logEvent.UserStackFrame != null)
            {
                propertyBag.Add("UserStackFrame", logEvent.UserStackFrame.ToString());
                propertyBag.Add("UserStackFrameNumber", logEvent.UserStackFrameNumber.ToString(CultureInfo.InvariantCulture));
            }
        }

        private SeverityLevel? GetSeverityLevel(LogLevel logEventLevel)
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
    }
}