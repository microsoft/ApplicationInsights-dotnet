using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace BackgroundTasksWithHostedService
{
    public class MyCustomTelemetryProcessor : ITelemetryProcessor
    {
        ITelemetryProcessor next;

        public MyCustomTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;

        }
        public void Process(ITelemetry item)
        {
            // Example processor - not filtering out anything.
            // This should be replaced with actual logic.
            this.next.Process(item);
        }
    }
}
