namespace Microsoft.ApplicationInsights.Web.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    
    public class SimpleTelemetryProcessorSpy : ITelemetryProcessor
    {
        private List<Channel.ITelemetry> receivedItems = new List<ITelemetry>();

        public SimpleTelemetryProcessorSpy()
        {
        }

        public int ReceivedCalls
        {
            get
            {
                return this.receivedItems.Count;
            }
        }

        public List<Channel.ITelemetry> ReceivedItems
        {
            get
            {
                return this.receivedItems;
            }
        } 

        public void Process(Channel.ITelemetry item)
        {
            this.receivedItems.Add(item);
        }
    }
}
