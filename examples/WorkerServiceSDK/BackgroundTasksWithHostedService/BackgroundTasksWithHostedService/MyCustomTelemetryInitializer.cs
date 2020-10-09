using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace BackgroundTasksWithHostedService
{
    public class MyCustomTelemetryInitializer : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            // Replace with actual properties.
            (telemetry as ISupportProperties).Properties["MyCustomKey"] = "MyCustomValue";
        }
    }
}
