using System;
using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal static class Util
    {
        public const string NullString = "null";
        private const double MicroOne = 0.000001;

        private const string FallbackParemeterName = "specified parameter";
        private const string MetricsSdkVersionMonikerPrefix = "msdk-";

        private static Action<TelemetryContext, TelemetryContext, string> s_delegateTelemetryContextInitialize = null;
        private static Func<TelemetryClient, TelemetryConfiguration> s_delegateTelemetryClientGetConfiguration = null;

        private static string s_sdkVersionMoniker = null;

        /// <summary>
        /// Paramater check for Null with a little more informative exception.
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
        /// String paramater check with a little more informative exception.
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
                throw new ArgumentException($"{name ?? Util.FallbackParemeterName} may not be empty.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double EnsureConcreteValue(double x)
        {
            return (x < -Double.MaxValue)
                        ? -Double.MaxValue
                        : (x > Double.MaxValue)
                                ? Double.MaxValue
                                : (Double.IsNaN(x))
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
                                          +$" a negative double value ({value})."
                                          + " Have you specified the correct metric configuration?");
            }

            double wholeValue = Math.Round(value);

            if (wholeValue > UInt32.MaxValue)
            {
                throw new ArgumentException("Cannot process the specified value."
                                         + " A non-negavite whole number was expected, but the specified value is"
                                         +$" larger than the maximum accepted value ({value})."
                                         + " Have you specified the correct metric configuration?");
            }

            double delta = Math.Abs(value - wholeValue);
            if (delta > MicroOne)
            {
                throw new ArgumentException("Cannot process the specified value."
                                          + " A non-negavite whole number was expected, but the specified value is"
                                          +$" a double value that does not equal to a whole number ({value})."
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
                return (double) (SByte) metricValue;
            }
            else if (metricValue is Byte)
            {
                return (double) (Byte) metricValue;
            }
            else if (metricValue is Int16)
            {
                return (double) (Int16) metricValue;
            }
            else if (metricValue is UInt16)
            {
                return (double) (UInt16) metricValue;
            }
            else if (metricValue is Int32)
            {
                return (double) (Int32) metricValue;
            }
            else if (metricValue is UInt32)
            {
                return (double) (UInt32) metricValue;
            }
            else if (metricValue is Int64)
            {
                return (double) (Int64) metricValue;
            }
            else if (metricValue is UInt64)
            {
                return (double) (UInt64) metricValue;
            }
            else if (metricValue is Single)
            {
                return (double) (Single) metricValue;
            }
            else if (metricValue is Double)
            {
                return (double) (Double) metricValue;
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
                                                  +$" A numeric value was expected, but the specified {nameof(metricValue)} is"
                                                  +$" a String that cannot be parsed into a number (\"{metricValue}\")."
                                                  + " Have you specified the correct metric configuration?");
                    }
                }
                else
                {
                    throw new ArgumentException("Cannot process the specified value."
                                             +$" A numeric value was expected, but the specified {nameof(metricValue)} is"
                                             +$" of type {metricValue.GetType().FullName}."
                                             + " Have you specified the correct metric configuration?");
                }
            }
        }
    }
}
