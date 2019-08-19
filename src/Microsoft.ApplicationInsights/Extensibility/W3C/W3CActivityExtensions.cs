namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Extends Activity to support W3C distributed tracing standard.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C making extension methods in this class no longer required.")]
    public static class W3CActivityExtensions
    {
        /// <summary>
        /// Generate new W3C context.
        /// </summary>
        /// <param name="activity">Activity to generate W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static Activity GenerateW3CContext(this Activity activity)
        {
            // No-op
            return activity;
        }

        /// <summary>
        /// Checks if current Activity has W3C properties on it.
        /// </summary>
        /// <param name="activity">Activity to check.</param>
        /// <returns>True if Activity has W3C properties, false otherwise.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static bool IsW3CActivity(this Activity activity)
        {            
            return activity != null && Activity.DefaultIdFormat.Equals(ActivityIdFormat.W3C);
        }

        /// <summary>
        /// Updates context on the Activity based on the W3C Context in the parent Activity tree.
        /// </summary>
        /// <param name="activity">Activity to update W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static Activity UpdateContextOnActivity(this Activity activity)
        {
            // No-op
            return activity;
        }

        /// <summary>
        /// Gets traceparent header value for the Activity or null if there is no W3C context on it.
        /// </summary>
        /// <param name="activity">Activity to read W3C context from.</param>
        /// <returns>traceparent header value.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static string GetTraceparent(this Activity activity)
        {
            // no-op as there is no native way to obtain TraceParent from Activity. 
            return string.Empty;

            /* OR //TODO decide if we want to keep this no-op
            var traceId = activity.TraceId.ToHexString();
            var spanId = activity.SpanId.ToHexString();
            if (traceId.Equals("00000000000000000000000000000000") || spanId.Equals("0000000000000000"))
            {
                return null;
            }

           return string.Join("-", W3CConstants.DefaultVersion, traceId, spanId, activity.Recorded ? "00" : "01");
           */
        }

        /// <summary>
        /// Initializes W3C context on the Activity from traceparent header value.
        /// </summary>
        /// <param name="activity">Activity to set W3C context on.</param>
        /// <param name="value">Valid traceparent header like 00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static void SetTraceparent(this Activity activity, string value)
        {
            // no-op
        }

        /// <summary>
        /// Gets tracestate header value from the Activity.
        /// </summary>
        /// <param name="activity">Activity to get tracestate from.</param>
        /// <returns>tracestate header value.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static string GetTracestate(this Activity activity)
        {
            return activity.TraceStateString;
        }
         
        /// <summary>
        /// Sets tracestate header value on the Activity.
        /// </summary>
        /// <param name="activity">Activity to set tracestate on.</param>
        /// <param name="value">tracestate header value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        public static void SetTracestate(this Activity activity, string value)
        {
            activity.TraceStateString = value;
        }

        /// <summary>
        /// Gets TraceId from the Activity.
        /// </summary>
        /// <param name="activity">Activity to get traceId from.</param>
        /// <returns>TraceId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C. Use Activity.TraceId to get Trace ID")]
        public static string GetTraceId(this Activity activity)
        {
            return activity.TraceId.ToHexString();
        }

        /// <summary>
        /// Gets SpanId from the Activity.        
        /// </summary>
        /// <param name="activity">Activity to get spanId from.</param>
        /// <returns>SpanId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C. Use Activity.SpanId to get Span ID")]
        public static string GetSpanId(this Activity activity)
        {
            return activity.SpanId.ToHexString();
        }

        /// <summary>
        /// Gets ParentSpanId from the Activity.
        /// </summary>
        /// <param name="activity">Activity to get ParentSpanId from.</param>
        /// <returns>ParentSpanId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C. Use Activity.ParentSpanId to get ParentSpan ID")]
        public static string GetParentSpanId(this Activity activity)
        {
            return activity.ParentSpanId.ToHexString();
        }

        /// <summary>
        /// Sets Activity W3C context on the telemetry.
        /// </summary>
        /// <param name="activity">Activity to update telemetry from.</param>
        /// <param name="telemetry">Telemetry item.</param>
        /// <param name="forceUpdate">Force update if properties are already set.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C. OperationCorrelationTelemetryInitializer is W3C aware and is recommended to update telemetry from current Activity.")]
        public static void UpdateTelemetry(this Activity activity, ITelemetry telemetry, bool forceUpdate)
        {
            // no-op
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Activity from System.Diagnostics.DiagnosticSource 4.6.0 onwards natively support W3C.")]
        internal static void SetParentSpanId(this Activity activity, string value)
        {
            // no-op
        }        
    }
}
