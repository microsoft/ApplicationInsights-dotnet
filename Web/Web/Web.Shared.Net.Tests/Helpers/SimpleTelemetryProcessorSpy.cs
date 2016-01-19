namespace Microsoft.ApplicationInsights.Web.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.Extensibility;
    
    public class SimpleTelemetryProcessorSpy : ITelemetryProcessor
    {
        private int processCalls;

        public SimpleTelemetryProcessorSpy()
        {
        }

        public int ReceivedCalls
        {
            get
            {
                return this.processCalls;
            }
        }

        public void Process(Channel.ITelemetry item)
        {
            this.processCalls++;
        }
    }
}
