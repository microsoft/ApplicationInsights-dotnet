namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    internal static class Telemetry
    {
        public static void WriteEnvelopeProperties(this ITelemetry telemetry, ISerializationWriter json)
        {
            json.WriteProperty("time", telemetry.Timestamp.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));

            var samplingSupportingTelemetry = telemetry as ISupportSampling;

            if (samplingSupportingTelemetry != null
                && samplingSupportingTelemetry.SamplingPercentage.HasValue
                && (samplingSupportingTelemetry.SamplingPercentage.Value > 0.0 + 1.0E-12)
                && (samplingSupportingTelemetry.SamplingPercentage.Value < 100.0 - 1.0E-12))
            {
                json.WriteProperty("sampleRate", samplingSupportingTelemetry.SamplingPercentage.Value);
            }

            json.WriteProperty("seq", telemetry.Sequence);
            WriteTelemetryContext(json, telemetry.Context);
        }

        public static string WriteTelemetryName(this ITelemetry telemetry, string telemetryName)
        {
            // A different event name prefix is sent for normal mode and developer mode.
            // Format the event name using the following format:
            // Microsoft.ApplicationInsights[.Dev].<normalized-instrumentation-key>.<event-type>
            var eventName = string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}{2}",
                telemetry.IsDeveloperMode() ? Constants.DevModeTelemetryNamePrefix : Constants.TelemetryNamePrefix,
                NormalizeInstrumentationKey(telemetry.Context.InstrumentationKey),
                telemetryName);

            return eventName;
        }

        public static void WriteTelemetryContext(ISerializationWriter json, TelemetryContext context)
        {
            if (context != null)
            {
                json.WriteProperty("iKey", context.InstrumentationKey);
                if (context.Flags != 0)
                {
                    json.WriteProperty("flags", context.Flags);
                }

                json.WriteProperty("tags", context.SanitizedTags);
            }
        }

        /// <summary>
        /// Inspect if <see cref="ITelemetry"/> Properties contains 'DeveloperMode' and return it's boolean value.
        /// </summary>
        private static bool IsDeveloperMode(this ITelemetry telemetry)
        {
            if (telemetry is ISupportProperties telemetryWithProperties
                && telemetryWithProperties != null
                && telemetryWithProperties.Properties.TryGetValue("DeveloperMode", out string devModeProperty)
                && bool.TryParse(devModeProperty, out bool isDevMode))
            {
                return isDevMode;
            }

            return false;
        }

        /// <summary>
        /// Normalize instrumentation key by removing dashes ('-') and making string in the lowercase.
        /// In case no InstrumentationKey is available just return empty string.
        /// In case when InstrumentationKey is available return normalized key + dot ('.')
        /// as a separator between instrumentation key part and telemetry name part.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Implementation expects lower case")]
        private static string NormalizeInstrumentationKey(string instrumentationKey)
        {
            if (instrumentationKey.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            return instrumentationKey.Replace("-", string.Empty).ToLowerInvariant() + ".";
        }
    }
}
