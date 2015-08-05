namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Various utilities.
    /// </summary>
    internal static partial class Utils
    {
        public static bool IsNullOrWhiteSpace(this string value)
        {
            if (value == null)
            {
                return true;
            }
#if !CORE_PCL
            return value.All(char.IsWhiteSpace);
#else
            return string.IsNullOrWhiteSpace(value);
#endif
        }

        public static void CopyDictionary<TValue>(IDictionary<string, TValue> source, IDictionary<string, TValue> target)
        {
            foreach (KeyValuePair<string, TValue> pair in source)
            {
                if (string.IsNullOrEmpty(pair.Key))
                {
                    continue;
                }

                if (!target.ContainsKey(pair.Key))
                {
                    target[pair.Key] = pair.Value;
                }
            }
        }

        /// <summary>
        /// Validates the string and if null or empty populates it with '$parameterName is a required field for $telemetryType' value.
        /// </summary>
        public static string PopulateRequiredStringValue(string value, string parameterName, string telemetryType)
        {
            if (string.IsNullOrEmpty(value))
            {
                CoreEventSource.Log.PopulateRequiredStringWithValue(parameterName, telemetryType);
                return parameterName + " is a required field for " + telemetryType;
            }

            return value;
        }

        /// <summary>
        /// Returns default Timespan value if not a valid Timespan.
        /// </summary>
        public static TimeSpan ValidateDuration(string value)
        {
            TimeSpan interval;
#if NET45 || UWP || NET46
            if (!TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out interval))
#else
            if (!TimeSpanEx.TryParse(value, CultureInfo.InvariantCulture, out interval))
#endif
            {
                CoreEventSource.Log.RequestTelemetryIncorrectDuration();
                return TimeSpan.Zero;
            }

            return interval;
        }

        internal static bool EqualsWithPrecision(this double value1, double value2, double precision)
        {
            return (value1 >= value2 - precision) && (value1 <= value2 + precision);
        }
    }
}
