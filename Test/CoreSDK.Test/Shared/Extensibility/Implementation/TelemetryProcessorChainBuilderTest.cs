namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
#if CORE_PCL || NET45 || WINRT || NET46 
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;    

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    [TestClass]
    public class TelemetryProcessorChainBuilderTest

    {
        [TestMethod]
        public void ThrowsInvalidOperationExceptionOnReturningNullFromUse()
        {
            var builder = new TelemetryProcessorChainBuilder(new TelemetryConfiguration());
            builder.Use((next) => null);

            Assert.Throws<InvalidOperationException>(() => builder.Build());
            
        }

        [TestMethod]
        public void TransmissionProcessorIsAddedDefaultWhenNoOtherTelemetryProcessorsAreConfigured()
        {
            var config = new TelemetryConfiguration();
            var builder = new TelemetryProcessorChainBuilder(config);            
            builder.Build();
            Assert.IsType<TransmissionProcessor>(config.TelemetryProcessors.FirstTelemetryProcessor);
        }

        [TestMethod]
        public void UsesTelemetryProcessorGivenInUseToBuild()
        {
            var config = new TelemetryConfiguration();
            var builder = new TelemetryProcessorChainBuilder(config);
            builder.Use((next) => new StubTelemetryProcessor(next));                    
            builder.Build();
            Assert.IsType<StubTelemetryProcessor>(config.TelemetryProcessors.FirstTelemetryProcessor);
        }

        [TestMethod]
        public void BuildUsesTelemetryProcesorFactoryOnEachCall()
        {
            var tc1 = new TelemetryConfiguration();
            var tc2 = new TelemetryConfiguration();
            var builder1 = new TelemetryProcessorChainBuilder(tc1);
            builder1.Use((next) => new StubTelemetryProcessor(next));
            builder1.Build();

            var builder2 = new TelemetryProcessorChainBuilder(tc2);
            builder2.Use((next) => new StubTelemetryProcessor(next));
            builder2.Build();
            
            Assert.NotSame(tc1.TelemetryProcessors, tc2.TelemetryProcessors);
        }

        [TestMethod]
        public void BuildOrdersTelemetryChannelsInOrderOfUseCalls()
        {
           var config = new TelemetryConfiguration() {TelemetryChannel = new StubTelemetryChannel()};
           StringBuilder outputCollector = new StringBuilder();
           var builder = new TelemetryProcessorChainBuilder(config);
           builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor1"); } });
           builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor2"); } });
           builder.Build();
           config.TelemetryProcessors.Process(new StubTelemetry());            

           Assert.Equal("processor1processor2", outputCollector.ToString());
        }
    }
}
