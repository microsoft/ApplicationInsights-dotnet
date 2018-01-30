namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
#if NETCORE
    using System.Reflection;
#endif
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
#if !NETCORE
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

    /// <summary>
    /// Part of the <see cref="QuickPulseTestHelper"/> class for all .NET Frameworks.
    /// </summary>
    internal static class QuickPulseTestHelper
    {
        public static LinkedList<IQuickPulseTelemetryProcessor> GetTelemetryProcessors(QuickPulseTelemetryModule module)
        {
            return GetPrivateField(module, "telemetryProcessors") as LinkedList<IQuickPulseTelemetryProcessor>;
        }

        public static void ClearEnvironment()
        {
            SetPrivateStaticField(typeof(TelemetryConfiguration), "active", null);
            TelemetryModules.Instance.Modules.Clear();
        }

        public static object GetPrivateField(object obj, string fieldName)
        {
#if NETCORE
            TypeInfo type = obj.GetType().GetTypeInfo();
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(obj);
#else
            var t = new PrivateObject(obj);
            return t.GetField(fieldName);
#endif
        }

        private static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
#if NETCORE
            TypeInfo typeInfo = type.GetTypeInfo();
            FieldInfo field = typeInfo.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            field.SetValue(null, value);
#else
            PrivateType t = new PrivateType(type);
            t.SetStaticField(fieldName, value);
#endif
        }
    }
}