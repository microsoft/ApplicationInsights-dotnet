namespace Microsoft.ApplicationInsights.W3C
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.ApplicationInsights.Common;

    /// <summary>
    /// Extends Activity to support W3C distributed tracing standard.
    /// </summary>
    [Obsolete("Not ready for public consumption.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class W3CActivityExtensions
    {
        private static readonly Regex TraceIdRegex = new Regex("^[a-f0-9]{32}$", RegexOptions.Compiled);
        private static readonly Regex SpanIdRegex = new Regex("^[a-f0-9]{16}$", RegexOptions.Compiled);

        /// <summary>
        /// Generate new W3C context.
        /// </summary>
        /// <param name="activity">Activity to generate W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Activity GenerateW3CContext(this Activity activity)
        {
            activity.SetVersion(W3CConstants.DefaultVersion);
            activity.SetSampled(W3CConstants.TraceFlagRecordedAndNotRequested);
            activity.SetSpanId(StringUtilities.GenerateSpanId());
            activity.SetTraceId(StringUtilities.GenerateTraceId());
            return activity;
        }

        /// <summary>
        /// Checks if current Activity has W3C properties on it.
        /// </summary>
        /// <param name="activity">Activity to check.</param>
        /// <returns>True if Activity has W3C properties, false otherwise.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsW3CActivity(this Activity activity)
        {
            return activity != null && activity.Tags.Any(t => t.Key == W3CConstants.TraceIdTag);
        }

        /// <summary>
        /// Updates context on the Activity based on the W3C Context in the parent Activity tree.
        /// </summary>
        /// <param name="activity">Activity to update W3C context on.</param>
        /// <returns>The same Activity for chaining.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Activity UpdateContextOnActivity(this Activity activity)
        {
            if (activity == null || activity.Tags.Any(t => t.Key == W3CConstants.TraceIdTag))
            {
                return activity;
            }

            // no w3c Tags on Activity
            activity.Parent.UpdateContextOnActivity();

            // at this point, Parent has W3C tags, but current activity does not - update it
            return activity.UpdateContextFromParent();
        }

        /// <summary>
        /// Gets traceparent header value for the Activity or null if there is no W3C context on it.
        /// </summary>
        /// <param name="activity">Activity to read W3C context from.</param>
        /// <returns>traceparent header value.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetTraceparent(this Activity activity)
        {
            string version = null, traceId = null, spanId = null, sampled = null;
            foreach (var tag in activity.Tags)
            {
                switch (tag.Key)
                {
                    case W3CConstants.TraceIdTag:
                        traceId = tag.Value;
                        break;
                    case W3CConstants.SpanIdTag:
                        spanId = tag.Value;
                        break;
                    case W3CConstants.VersionTag:
                        version = tag.Value;
                        break;
                    case W3CConstants.SampledTag:
                        sampled = tag.Value;
                        break;
                }
            }

            if (traceId == null || spanId == null || version == null || sampled == null)
            {
                return null;
            }

            return string.Join("-", version, traceId, spanId, sampled);
        }

        /// <summary>
        /// Initializes W3C context on the Activity from traceparent header value.
        /// </summary>
        /// <param name="activity">Activity to set W3C context on.</param>
        /// <param name="value">Valid traceparent header like 00-0af7651916cd43dd8448eb211c80319c-b9c7c989f97918e1-01.</param>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetTraceparent(this Activity activity, string value)
        {
            if (activity.IsW3CActivity())
            {
                return;
            }

            // we only support 00 version and ignore caller version
            activity.SetVersion(W3CConstants.DefaultVersion);

            string traceId = null, parentSpanId = null, sampledStr = null;
            bool isValid = false;

            var parts = value?.Split('-');
            if (parts != null && parts.Length == 4)
            {
                traceId = parts[1];
                parentSpanId = parts[2];
                sampledStr = parts[3];
                isValid = TraceIdRegex.IsMatch(traceId) && SpanIdRegex.IsMatch(parentSpanId);
            }

            if (isValid)
            {
                byte.TryParse(sampledStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var sampled);

                // we always defer sampling
                if ((sampled & W3CConstants.RequestedTraceFlag) == W3CConstants.RequestedTraceFlag)
                {
                    activity.SetSampled(W3CConstants.TraceFlagRecordedAndRequested);
                }
                else
                {
                    activity.SetSampled(W3CConstants.TraceFlagRecordedAndNotRequested);
                }

                activity.SetParentSpanId(parentSpanId);
                activity.SetSpanId(StringUtilities.GenerateSpanId());
                activity.SetTraceId(traceId);
            }
            else
            {
                activity.SetSampled(W3CConstants.TraceFlagRecordedAndNotRequested);
                activity.SetSpanId(StringUtilities.GenerateSpanId());
                activity.SetTraceId(StringUtilities.GenerateTraceId());
            }
        }

        /// <summary>
        /// Gets tracestate header value from the Activity.
        /// </summary>
        /// <param name="activity">Activity to get tracestate from.</param>
        /// <returns>tracestate header value.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetTracestate(this Activity activity) =>
            activity.Tags.FirstOrDefault(t => t.Key == W3CConstants.TracestateTag).Value;

        /// <summary>
        /// Sets tracestate header value on the Activity.
        /// </summary>
        /// <param name="activity">Activity to set tracestate on.</param>
        /// <param name="value">tracestate header value.</param>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetTracestate(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.TracestateTag, value);

        /// <summary>
        /// Gets TraceId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get traceId from.</param>
        /// <returns>TraceId value or null if it does not exist.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetTraceId(this Activity activity) => activity.Tags.FirstOrDefault(t => t.Key == W3CConstants.TraceIdTag).Value;

        /// <summary>
        /// Gets SpanId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get spanId from.</param>
        /// <returns>SpanId value or null if it does not exist.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetSpanId(this Activity activity) => activity.Tags.FirstOrDefault(t => t.Key == W3CConstants.SpanIdTag).Value;

        /// <summary>
        /// Gets ParentSpanId from the Activity.
        /// Use carefully: if may cause iteration over all tags!
        /// </summary>
        /// <param name="activity">Activity to get ParentSpanId from.</param>
        /// <returns>ParentSpanId value or null if it does not exist.</returns>
        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static string GetParentSpanId(this Activity activity) => activity.Tags.FirstOrDefault(t => t.Key == W3CConstants.ParentSpanIdTag).Value;

        [Obsolete("Not ready for public consumption.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void SetParentSpanId(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.ParentSpanIdTag, value);

        private static void SetTraceId(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.TraceIdTag, value);

        private static void SetSpanId(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.SpanIdTag, value);

        private static void SetVersion(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.VersionTag, value);

        private static void SetSampled(this Activity activity, string value) =>
            activity.AddTag(W3CConstants.SampledTag, value);

        private static Activity UpdateContextFromParent(this Activity activity)
        {
            if (activity != null && activity.Tags.All(t => t.Key != W3CConstants.TraceIdTag))
            {
                if (activity.Parent == null)
                {
                    activity.GenerateW3CContext();
                }
                else
                {
                    foreach (var tag in activity.Parent.Tags)
                    {
                        switch (tag.Key)
                        {
                            case W3CConstants.TraceIdTag:
                                activity.SetTraceId(tag.Value);
                                break;
                            case W3CConstants.SpanIdTag:
                                activity.SetParentSpanId(tag.Value);
                                activity.SetSpanId(StringUtilities.GenerateSpanId());
                                break;
                            case W3CConstants.VersionTag:
                                activity.SetVersion(tag.Value);
                                break;
                            case W3CConstants.SampledTag:
                                activity.SetSampled(tag.Value);
                                break;
                            case W3CConstants.TracestateTag:
                                activity.SetTracestate(tag.Value);
                                break;
                        }
                    }
                }
            }

            return activity;
        }
    }
}
