using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Metrics
{
    internal static class Util
    {
        public const string NullString = "null";

        private const string FallbackParemeterName = "specified parameter";

        private static Action<TelemetryContext, TelemetryContext, string> s_telemetryContextInitializeDelegate = null;

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
            Action<TelemetryContext, TelemetryContext, string> initializeDelegate = GetTelemetryContextInitializeDelegate();
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
        }

        private static Action<TelemetryContext, TelemetryContext, string> GetTelemetryContextInitializeDelegate()
        {
            //Need to invoke: void TelemetryContext.Initialize(TelemetryContext source, string instrumentationKey)

            Action<TelemetryContext, TelemetryContext, string> currentDel = s_telemetryContextInitializeDelegate;

            if (currentDel == null)
            {
                MethodInfo initializeMethod = typeof(TelemetryContext).GetTypeInfo().GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance);

                Action<TelemetryContext, TelemetryContext, string> newDel =
                                            (Action<TelemetryContext, TelemetryContext, string>)
                                            initializeMethod.CreateDelegate(typeof(Action<TelemetryContext, TelemetryContext, string>));

                Action<TelemetryContext, TelemetryContext, string> prevDel = Interlocked.CompareExchange(ref s_telemetryContextInitializeDelegate, newDel, null);
                currentDel = prevDel ?? newDel;
            }

            return currentDel;
        }
    }
}
