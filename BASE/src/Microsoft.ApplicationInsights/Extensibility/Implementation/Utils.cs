namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    /// <summary>
    /// Various utilities.
    /// </summary>
    internal static partial class Utils
    {
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
                return "n/a";
            }

            return value;
        }
    }
}
