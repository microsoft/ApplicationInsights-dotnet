namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;

    /// <summary>
    /// Part of the <see cref="QuickPulseTestHelper"/> class for all .NET Frameworks.
    /// </summary>
    internal static partial class QuickPulseTestHelper
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
    }
}