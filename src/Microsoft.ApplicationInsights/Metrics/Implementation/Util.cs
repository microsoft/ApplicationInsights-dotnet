namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Metrics.Extensibility;

    internal static class Util
    {
        public const string NullString = "null";
        private const double MicroOne = 0.000001;

        private const string FallbackParemeterName = "specified parameter";

        private static string sdkVersionMoniker = SdkVersionUtils.GetSdkVersion("m-agg2:");

        /// <summary>
        /// Paramater check for Null.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <param name="name">Name of the parameter being checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateNotNull(object value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name ?? Util.FallbackParemeterName);
            }
        }

        /// <summary>
        /// String paramater check with a more informative exception that specifies whether
        /// the problem was that the string was null or empty.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <param name="name">Name of the parameter being checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateNotNullOrEmpty(string value, string name)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name ?? Util.FallbackParemeterName);
            }

            if (value.Length == 0)
            {
                throw new ArgumentException((name ?? Util.FallbackParemeterName) + " may not be empty.");
            }
        }

        /// <summary>
        /// String paramater check with a more informative exception that specifies whether
        /// the problem was that the string was null, empty or whitespace only.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <param name="name">Name of the parameter being checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateNotNullOrWhitespace(string value, string name)
        {
            ValidateNotNullOrEmpty(value, name);

            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException((name ?? Util.FallbackParemeterName) + " may not be whitespace only.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EnsureConcreteValue(double x)
        {
            return (x < -Double.MaxValue)
                        ? -Double.MaxValue
                        : (x > Double.MaxValue)
                                ? Double.MaxValue
                                : Double.IsNaN(x)
                                        ? 0.0
                                        : x;
        }

        public static double RoundAndValidateValue(double value)
        {
            if (Double.IsNaN(value))
            {
                throw new ArgumentException("Cannot process the specified value."
                                          + " A non-negavite whole number was expected, but the specified value is Double.NaN."
                                          + " Have you specified the correct metric configuration?");
            }

            if (value < -MicroOne)
            {
                throw new ArgumentException("Cannot process the specified value."
                                          + " A non-negavite whole number was expected, but the specified value is"
                                          + " a negative double value (" + value + ")."
                                          + " Have you specified the correct metric configuration?");
            }

            double wholeValue = Math.Round(value);

            if (wholeValue > UInt32.MaxValue)
            {
                throw new ArgumentException("Cannot process the specified value."
                                         + " A non-negavite whole number was expected, but the specified value is"
                                         + " larger than the maximum accepted value (" + value + ")."
                                         + " Have you specified the correct metric configuration?");
            }

            double delta = Math.Abs(value - wholeValue);
            if (delta > MicroOne)
            {
                throw new ArgumentException("Cannot process the specified value."
                                          + " A non-negavite whole number was expected, but the specified value is"
                                          + " a double value that does not equal to a whole number (" + value + ")."
                                          + " Have you specified the correct metric configuration?");
            }

            return wholeValue;
        }

        public static double ConvertToDoubleValue(object metricValue)
        {
            if (metricValue == null)
            {
                return Double.NaN;
            }

            if (metricValue is SByte)
            {
                return (double)(SByte)metricValue;
            }
            else if (metricValue is Byte)
            {
                return (double)(Byte)metricValue;
            }
            else if (metricValue is Int16)
            {
                return (double)(Int16)metricValue;
            }
            else if (metricValue is UInt16)
            {
                return (double)(UInt16)metricValue;
            }
            else if (metricValue is Int32)
            {
                return (double)(Int32)metricValue;
            }
            else if (metricValue is UInt32)
            {
                return (double)(UInt32)metricValue;
            }
            else if (metricValue is Int64)
            {
                return (double)(Int64)metricValue;
            }
            else if (metricValue is UInt64)
            {
                return (double)(UInt64)metricValue;
            }
            else if (metricValue is Single)
            {
                return (double)(Single)metricValue;
            }
            else if (metricValue is Double)
            {
                return (double)(Double)metricValue;
            }
            else
            {
                string stringValue = metricValue as string;
                if (stringValue != null)
                {
                    double doubleValue;
                    if (Double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        return doubleValue;
                    }
                    else
                    {
                        throw new ArgumentException("Cannot process the specified value."
                                                  + " A numeric value was expected, but the specified " + nameof(metricValue) + " is"
                                                  + " a String that cannot be parsed into a number (\"" + metricValue + "\")."
                                                  + " Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException("Cannot process the specified value."
                                              + " A numeric value was expected, but the specified " + nameof(metricValue) + " is"
                                              + " of type " + metricValue.GetType().FullName + "."
                                              + " Have you specified the correct metric configuration?");
                }
            }
        }

        public static DateTimeOffset RoundDownToMinute(DateTimeOffset dto)
        {
            return new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, 0, 0, dto.Offset);
        }

        public static DateTimeOffset RoundDownToSecond(DateTimeOffset dto)
        {
            return new DateTimeOffset(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, 0, dto.Offset);
        }

        public static bool FilterWillConsume(IMetricSeriesFilter seriesFilter, MetricSeries series, out IMetricValueFilter valueFilter)
        {
            valueFilter = null;
            try
            {
                return (seriesFilter == null) || seriesFilter.WillConsume(series, out valueFilter);
            }
            catch
            {
                // Protect against errors in user's implemenmtation of IMetricSeriesFilter.WillConsume(..).
                // If it throws, assume that the filter is not functional and accept all values.
                return true;
            }
        }

        public static bool FilterWillConsume(IMetricValueFilter valueFilter, MetricSeries series, double metricValue)
        {
            try
            {
                return (valueFilter == null) || valueFilter.WillConsume(series, metricValue);
            }
            catch
            {
                // If user code in IMetricValueFilter.WillConsume(..) throws, assume that the filter is not functional and accept all values.
                return true;
            }
        }

        public static bool FilterWillConsume(IMetricValueFilter valueFilter, MetricSeries series, object metricValue)
        {
            try
            {
                return (valueFilter == null) || valueFilter.WillConsume(series, metricValue);
            }
            catch
            {
                // If user code in IMetricValueFilter.WillConsume(..) throws, assume that the filter is not functional and accept all values.
                return true;
            }
        }

        public static int CombineHashCodes(int hash1)
        {
            int hash = 17;
            unchecked
            {
                hash = (hash * 23) + hash1;
            }

            return hash;
        }

        public static int CombineHashCodes(int hash1, int hash2)
        {
            int hash = 17;
            unchecked
            {
                hash = (hash * 23) + hash1;
                hash = (hash * 23) + hash2;
            }

            return hash;
        }

        public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4)
        {
            int hash = 17;
            unchecked
            {
                hash = (hash * 23) + hash1;
                hash = (hash * 23) + hash2;
                hash = (hash * 23) + hash3;
                hash = (hash * 23) + hash4;
            }

            return hash;
        }

        /// <summary>@ToDo: Complete documentation before stable release. {659}</summary>
        /// <param name="source">@ToDo: Complete documentation before stable release. {688}</param>
        /// <param name="target">@ToDo: Complete documentation before stable release. {859}</param>
        public static void CopyTelemetryContext(TelemetryContext source, TelemetryContext target)
        {
            Util.ValidateNotNull(source, nameof(source));
            Util.ValidateNotNull(target, nameof(target));

            // Copy internal tags:
            target.Initialize(source, instrumentationKey: null);

            // Copy public properties:
            IDictionary<string, string> sourceProperties = source.Properties;
            IDictionary<string, string> targetProperties = target.Properties;
            if (targetProperties != null && sourceProperties != null && sourceProperties.Count > 0)
            {
                foreach (KeyValuePair<string, string> property in sourceProperties)
                {
                    if (false == String.IsNullOrEmpty(property.Key) && false == targetProperties.ContainsKey(property.Key))
                    {
                        targetProperties[property.Key] = property.Value;
                    }
                }
            }

            // Copy iKey:

            if (source.InstrumentationKey != null)
            {
                target.InstrumentationKey = source.InstrumentationKey;
            }
        }

        public static void StampSdkVersionToContext(ITelemetry aggregate)
        {
            InternalContext context = aggregate?.Context?.GetInternalContext();

            if (context == null)
            {
                return;
            }

            context.SdkVersion = sdkVersionMoniker;
        }
    }
}
