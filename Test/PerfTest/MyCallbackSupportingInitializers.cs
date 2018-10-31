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
    public class MyCallbackSupportingInitializers : ITelemetryInitializer
    {
        public void Initialize(ITelemetry telemetry)
        {
            string stringFromHeader = "ValueFromheader";
            var depTelemetry = telemetry as DependencyTelemetry;
            if(depTelemetry != null)
            {
                depTelemetry.Callbacks.Add(
                        () => 
                        {
                            telemetry.Context.Device.Id = stringFromHeader;
                        }
                    );
            }

        }
    }
}
