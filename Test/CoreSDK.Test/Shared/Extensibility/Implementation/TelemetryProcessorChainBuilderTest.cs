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
            var builder = new TelemetryProcessorChainBuilder();
            builder.Use((next) => null);

            Assert.Throws<InvalidOperationException>(() => builder.Build(new TelemetryConfiguration()));
            
        }

        [TestMethod]
        public void TransmissionProcessorIsAddedDefaultWhenNoOtherTelemetryProcessorsAreConfigured()
        {
            var builder = new TelemetryProcessorChainBuilder();
            var config = new TelemetryConfiguration();

            builder.Build(config);
            Assert.IsType<TransmissionProcessor>(config.TelemetryProcessorChain.TelemetryProcessors.First());
        }

        [TestMethod]
        public void UsesTelemetryProcessorGivenInUseToBuild()
        {
            var builder = new TelemetryProcessorChainBuilder();
            builder.Use((next) => new StubTelemetryProcessor(next));
            var config = new TelemetryConfiguration();

            builder.Build(config);
            Assert.IsType<StubTelemetryProcessor>(config.TelemetryProcessorChain.TelemetryProcessors.First());
        }

        [TestMethod]
        public void BuildUsesTelemetryProcesorFactoryOnEachCall()
        {
            var tc1 = new TelemetryConfiguration();
            var tc2 = new TelemetryConfiguration();
            var builder = new TelemetryProcessorChainBuilder();
            builder.Use((next) => new StubTelemetryProcessor(next));

            builder.Build(tc1);
            builder.Build(tc2);

            Assert.NotSame(tc1.TelemetryProcessorChain, tc2.TelemetryProcessorChain);
        }

        [TestMethod]
        public void BuildOrdersTelemetryChannelsInOrderOfUseCalls()
        {
           var config = new TelemetryConfiguration() {TelemetryChannel = new StubTelemetryChannel()};
           StringBuilder outputCollector = new StringBuilder();
           var builder = new TelemetryProcessorChainBuilder();
           builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor1"); } });
           builder.Use((next) => new StubTelemetryProcessor(next) { OnProcess = (item) => { outputCollector.Append("processor2"); } });
           builder.Build(config);
           config.TelemetryProcessorChain.Process(new StubTelemetry());            

           Assert.Equal("processor1processor2", outputCollector.ToString());
        }
    }
}
