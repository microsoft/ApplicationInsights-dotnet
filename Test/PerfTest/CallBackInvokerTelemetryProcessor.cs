using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfTest
{
    public class CallBackInvokerTelemetryProcessor : ITelemetryProcessor
    {
        ITelemetryProcessor next;
        public CallBackInvokerTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;
        }

        public void Process(ITelemetry telemetry)
        {
            var depTelemetry = telemetry as DependencyTelemetry;
            if(depTelemetry != null)
            {
                foreach(var callback in depTelemetry.Callbacks)
                {
                    callback();
                }
            }

            this.next.Process(telemetry);
        }
    }
}
