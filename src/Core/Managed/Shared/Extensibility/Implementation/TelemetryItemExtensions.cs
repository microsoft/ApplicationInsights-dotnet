namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;

    internal static class TelemetryItemExtensions
    {
        internal static string GetTelemetryFullName(this ITelemetry item, string envelopeName)
        {
            return Constants.TelemetryNamePrefix + item.Context.InstrumentationKey + "|" + envelopeName;
        }
    }
}
