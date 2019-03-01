namespace Microsoft.ApplicationInsights.Extensibility.W3C
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.ApplicationInsights.Channel;

    /// <summary>
    /// Extends Activity to support W3C distributed tracing standard.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class W3CActivityExtensions
    {
        /// <summary>
        /// Generate new W3C context.
        /// </summary>
        /// <param name="activity">Activity to generate W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static Activity GenerateW3CContext(this Activity activity)
        {
            return activity;
        }

        /// <summary>
        /// Checks if current Activity has W3C properties on it.
        /// </summary>
        /// <param name="activity">Activity to check.</param>
        /// <returns>True if Activity has W3C properties, false otherwise.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static bool IsW3CActivity(this Activity activity)
        {
            return true;
        }

        /// <summary>
        /// Updates context on the Activity based on the W3C Context in the parent Activity tree.
        /// </summary>
        /// <param name="activity">Activity to update W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static Activity UpdateContextOnActivity(this Activity activity)
        {
            return activity;
        }

        /// <summary>
        /// Gets traceparent header value for the Activity or null if there is no W3C context on it.
        /// </summary>
        /// <param name="activity">Activity to read W3C context from.</param>
        /// <returns>traceparent header value.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static string GetTraceparent(this Activity activity)
        {
            return activity.Id;
        }

        /// <summary>
        /// Initializes W3C context on the Activity from traceparent header value.
        /// </summary>
        /// <param name="activity">Activity to set W3C context on.</param>
        /// <param name="value">Valid traceparent header like 00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static void SetTraceparent(this Activity activity, string value)
        {
            if (activity.Id == null)
            {
                activity.SetParentId(value);
            }
        }

        /// <summary>
        /// Gets tracestate header value from the Activity.
        /// </summary>
        /// <param name="activity">Activity to get tracestate from.</param>
        /// <returns>tracestate header value.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static string GetTracestate(this Activity activity) => activity.TraceStateString;

        /// <summary>
        /// Sets tracestate header value on the Activity.
        /// </summary>
        /// <param name="activity">Activity to set tracestate on.</param>
        /// <param name="value">tracestate header value.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static void SetTracestate(this Activity activity, string value)
        {
            activity.TraceStateString = value;
        }

        /// <summary>
        /// Gets TraceId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get traceId from.</param>
        /// <returns>TraceId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static string GetTraceId(this Activity activity) => activity.TraceId.AsHexString;

        /// <summary>
        /// Gets SpanId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get spanId from.</param>
        /// <returns>SpanId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static string GetSpanId(this Activity activity) => activity.SpanId.AsHexString;

        /// <summary>
        /// Gets ParentSpanId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get ParentSpanId from.</param>
        /// <returns>ParentSpanId value or null if it does not exist.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static string GetParentSpanId(this Activity activity) => activity.ParentSpanId.AsHexString;

        /// <summary>
        /// Sets Activity W3C context on the telemetry.
        /// </summary>
        /// <param name="activity">Activity to update telemetry from.</param>
        /// <param name="telemetry">Telemetry item.</param>
        /// <param name="forceUpdate">Force update if properties are already set.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", Justification =
            "This method has different code for Net45/NetCore")]
        [Obsolete("123")]
        [CLSCompliant(false)]
        public static void UpdateTelemetry(this Activity activity, ITelemetry telemetry, bool forceUpdate)
        {
            // ignore
        }
    }
}
