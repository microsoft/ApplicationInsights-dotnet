namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryProcessorChainTests
    {
        [TestMethod]
        public void VerefyTelemetryProcessorChainHandlesExceptions()
        {
            var telemetryProcessor = new ExceptionTelemetryProcessor();

            var processorChain = new TelemetryProcessorChain(new ITelemetryProcessor[] { telemetryProcessor});

            var telemetry = new EventTelemetry("test telemetry");

            // Verify Does Not Throw
            processorChain.Process(telemetry);

            // Verify Processor was invoked
            Assert.AreEqual(1, telemetryProcessor.InvokedCount);
        }

        internal class ExceptionTelemetryProcessor : ITelemetryProcessor
        {
            public int InvokedCount { get; set; }

            public void Process(ITelemetry item)
            {
                this.InvokedCount++;
                throw new Exception("test");
            }
        }
    }
}
