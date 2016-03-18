namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal static class QuickPulseTestHelper
    {
        public static object GetPrivateField(object obj, string fieldName)
        {
            var t = new PrivateObject(obj);
            return t.GetField(fieldName);
        }

        public static void SetPrivateStaticField(Type type, string fieldName, object value)
        {
            PrivateType t = new PrivateType(type);
            t.SetStaticField(fieldName, value);
        }

        public static List<IQuickPulseTelemetryProcessor> GetTelemetryProcessors(QuickPulseTelemetryModule module)
        {
            return GetPrivateField(module, "telemetryProcessors") as List<IQuickPulseTelemetryProcessor>;
        }

        public static void ClearEnvironment()
        {
            SetPrivateStaticField(typeof(TelemetryConfiguration), "active", null);
            TelemetryModules.Instance.Modules.Clear();
        }
    }
}