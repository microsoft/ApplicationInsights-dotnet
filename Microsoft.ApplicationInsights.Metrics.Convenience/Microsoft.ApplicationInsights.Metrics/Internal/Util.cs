using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal static class Util
    {
        private const string FallbackParemeterName = "specified parameter";

        private static Func<TelemetryClient, TelemetryConfiguration> s_telemetryClientConfigurationDelegate = null;

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

        /// <summary>
        /// We are working on adding a publically exposed method to a future version of the Core SDK so that the reflection employed here is not necesary.
        /// </summary>
        /// <param name="telemetryClient"></param>
        /// <returns></returns>
        internal static TelemetryConfiguration GetTelemetryConfiguration(TelemetryClient telemetryClient)
        {
            Util.ValidateNotNull(telemetryClient, nameof(telemetryClient));

            Func<TelemetryClient, TelemetryConfiguration> getTelemetryConfigurationDelegate = GetTelemetryClientConfigurationDelegate();
            TelemetryConfiguration pipeline = getTelemetryConfigurationDelegate(telemetryClient);

            return pipeline;
        }

        private static Func<TelemetryClient, TelemetryConfiguration> GetTelemetryClientConfigurationDelegate()
        {
            Func<TelemetryClient, TelemetryConfiguration> currentDel = s_telemetryClientConfigurationDelegate;

            if (currentDel == null)
            {
                PropertyInfo telemetryConfigurationProperty = typeof(TelemetryContext).GetTypeInfo().GetProperty(
                                                                                                                "TelemetryConfiguration",
                                                                                                                 BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo telemetryConfigurationGetMethod = telemetryConfigurationProperty.GetGetMethod(nonPublic: true);

                Func<TelemetryClient, TelemetryConfiguration> newDel =
                                            (Func<TelemetryClient, TelemetryConfiguration>)
                                             telemetryConfigurationGetMethod.CreateDelegate(typeof(Func<TelemetryClient, TelemetryConfiguration>));

                Func<TelemetryClient, TelemetryConfiguration> prevDel = Interlocked.CompareExchange(ref s_telemetryClientConfigurationDelegate, newDel, null);
                currentDel = prevDel ?? newDel;
            }

            return currentDel;
        }
    }
}
