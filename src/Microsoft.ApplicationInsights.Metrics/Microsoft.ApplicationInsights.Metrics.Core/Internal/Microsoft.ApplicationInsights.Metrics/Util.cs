using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

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

        /// <summary>
        /// String paramater check with a little more informative exception.
        /// </summary>
        /// <param name="value">Value to be checked.</param>
        /// <param name="name">Name of the parameter being checked.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateNotNullOrWhitespace(string value, string name)
        {
            ValidateNotNullOrEmpty(value, name);

            if (String.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"{name ?? Util.FallbackParemeterName} may not be whitespace only.");
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
                return (seriesFilter == null) || (seriesFilter.WillConsume(series, out valueFilter));
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
                return (valueFilter == null) || (valueFilter.WillConsume(series, metricValue));
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
                return (valueFilter == null) || (valueFilter.WillConsume(series, metricValue));
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
                hash = hash * 23 + hash1;
            }
            return hash;
        }

        public static int CombineHashCodes(int hash1, int hash2)
        {
            int hash = 17;
            unchecked
            {
                hash = hash * 23 + hash1;
                hash = hash * 23 + hash2;
            }
            return hash;
        }

        public static int CombineHashCodes(int hash1, int hash2, int hash3, int hash4)
        {
            int hash = 17;
            unchecked
            {
                hash = hash * 23 + hash1;
                hash = hash * 23 + hash2;
                hash = hash * 23 + hash3;
                hash = hash * 23 + hash4;
            }
            return hash;
        }

        /// <summary>
        /// We are working on adding a publically exposed method to a future version of the Core SDK so that the reflection employed here is not necesary.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public static void CopyTelemetryContext(TelemetryContext source, TelemetryContext target)
        {
            Util.ValidateNotNull(source, nameof(source));
            Util.ValidateNotNull(target, nameof(target));

            // Copy internal tags:
            Action<TelemetryContext, TelemetryContext, string> initializeDelegate = GetDelegate_TelemetryContextInitialize();
            initializeDelegate(target, source, null);

            // Copy public properties:
            IDictionary<string, string> sourceProperties = source.Properties;
            IDictionary<string, string> targetProperties = target.Properties;
            if (targetProperties != null && sourceProperties != null && sourceProperties.Count > 0)
            {
                foreach (KeyValuePair<string, string> property in sourceProperties)
                {
                    if (! String.IsNullOrEmpty(property.Key) && ! targetProperties.ContainsKey(property.Key))
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

            string sdkVersionMoniker = GetSdkVersionMoniker();
            context.SdkVersion = sdkVersionMoniker;
        }

        /// <summary>
        /// ToDo: Modeled on a copy from Base AI SDK. Keep in sync until the Base SDK version can be exposed.
        /// { SdkVersionUtils.GetSdkVersion(string versionPrefix); }
        /// Format msdk-MAJOR.MINOR-REVISION:major.minor-revision.
        ///   where CAPs indicate metrics SDK version and lower indicares base SDK version.
        /// </summary>
        /// <returns>String representation of the version with prefix added.</returns>
        public static string GetSdkVersionMoniker()
        {
            string sdkVersionMoniker = s_sdkVersionMoniker;
            if (sdkVersionMoniker != null)
            {
                return sdkVersionMoniker;
            }

            string baseSdkVersionStr = typeof(Microsoft.ApplicationInsights.TelemetryClient)
                                        .GetTypeInfo()
                                        .Assembly
                                        .GetCustomAttributes<AssemblyFileVersionAttribute>()
                                        .FirstOrDefault()?
                                        .Version;

            string metricsSdkVersionStr = typeof(Microsoft.ApplicationInsights.Metrics.MetricManager)
                                        .GetTypeInfo()
                                        .Assembly
                                        .GetCustomAttributes<AssemblyFileVersionAttribute>()
                                        .FirstOrDefault()?
                                        .Version;

            Version baseSdkVersion = null;
            Version metricsSdkVersion = null;

            if (false == Version.TryParse(baseSdkVersionStr ?? String.Empty, out baseSdkVersion))
            {
                baseSdkVersion = null;
            }

            if (false == Version.TryParse(metricsSdkVersionStr ?? String.Empty, out metricsSdkVersion))
            {
                metricsSdkVersion = null;
            }

            string baseSdkPostfix = baseSdkVersion.Revision.ToString(CultureInfo.InvariantCulture);
            string metricsSdkPostfix = metricsSdkVersion.Revision.ToString(CultureInfo.InvariantCulture);

            string metricsSdkVersionMoniker = $"{MetricsSdkVersionMonikerPrefix}{metricsSdkVersion?.ToString(3) ?? "0"}-{metricsSdkPostfix}";
            string baseSdkVersionMoniker = $"{baseSdkVersion?.ToString(3) ?? "0"}-{baseSdkPostfix}";
            sdkVersionMoniker = $"{metricsSdkVersionMoniker}:{baseSdkVersionMoniker}";

            s_sdkVersionMoniker = sdkVersionMoniker;
            return sdkVersionMoniker;
        }

        /// <summary>
        /// We are working on adding a publically exposed method to a future version of the Core SDK so that the reflection employed here is not necesary.
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <returns></returns>
        internal static TelemetryConfiguration GetTelemetryConfiguration(TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            Func<TelemetryClient, TelemetryConfiguration> getTelemetryConfigurationDelegate = GetDelegate_TelemetryClientGetConfiguration();
            TelemetryConfiguration pipeline = getTelemetryConfigurationDelegate(telemetryClient);

            return pipeline;
        }

        private static Func<TelemetryClient, TelemetryConfiguration> GetDelegate_TelemetryClientGetConfiguration()
        {
            Func<TelemetryClient, TelemetryConfiguration> currentDel = s_delegateTelemetryClientGetConfiguration;

            if (currentDel == null)
            {
                Type apiType = typeof(TelemetryClient);
                const string apiName = "TelemetryConfiguration";
                PropertyInfo property = apiType.GetTypeInfo().GetProperty(apiName, BindingFlags.NonPublic | BindingFlags.Instance);

                if (property == null)
                {
                    throw new InvalidOperationException($"Could not get PropertyInfo for {apiType.Name}.{apiName} via reflection."
                                                       + " This is either an internal SDK bug or there is a mismatch between the Metrics-SDK version"
                                                       + " and the Application Insights Base SDK version. Please report this issue.");
                }

                MethodInfo propertyGetMethod = property.GetGetMethod(nonPublic: true);

                Func<TelemetryClient, TelemetryConfiguration> newDel =
                                            (Func<TelemetryClient, TelemetryConfiguration>)
                                             propertyGetMethod.CreateDelegate(typeof(Func<TelemetryClient, TelemetryConfiguration>));

                Func<TelemetryClient, TelemetryConfiguration> prevDel = Interlocked.CompareExchange(ref s_delegateTelemetryClientGetConfiguration, newDel, null);
                currentDel = prevDel ?? newDel;
            }

            return currentDel;
        }

        private static Action<TelemetryContext, TelemetryContext, string> GetDelegate_TelemetryContextInitialize()
        {
            //Need to invoke: void TelemetryContext.Initialize(TelemetryContext source, string instrumentationKey)

            Action<TelemetryContext, TelemetryContext, string> currentDel = s_delegateTelemetryContextInitialize;

            if (currentDel == null)
            {
                Type apiType = typeof(TelemetryContext);
                const string apiName = "Initialize";
                MethodInfo method = apiType.GetTypeInfo().GetMethod(apiName, BindingFlags.NonPublic | BindingFlags.Instance);

                if (method == null)
                {
                    throw new InvalidOperationException($"Could not get MethodInfo for {apiType.Name}.{apiName} via reflection."
                                                       + " This is either an internal SDK bug or there is a mismatch between the Metrics-SDK version"
                                                       + " and the Application Insights Base SDK version. Please report this issue.");
                }

                Action<TelemetryContext, TelemetryContext, string> newDel =
                                            (Action<TelemetryContext, TelemetryContext, string>)
                                            method.CreateDelegate(typeof(Action<TelemetryContext, TelemetryContext, string>));

                Action<TelemetryContext, TelemetryContext, string> prevDel = Interlocked.CompareExchange(ref s_delegateTelemetryContextInitialize, newDel, null);
                currentDel = prevDel ?? newDel;
            }

            return currentDel;
        }
    }
}
