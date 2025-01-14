namespace Microsoft.ApplicationInsights.Extensibility.Implementation;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Channel;


[TestClass]
public class TelemetryProcessorChainTests
{
    [TestMethod]
    public void ProcessorsAreDisposed()
    {
        var chain = new TelemetryProcessorChain();
        var processor = new DisposableProcessor();
        chain.TelemetryProcessors.Add(processor);
        chain.Dispose();
        Assert.IsTrue(processor.IsDisposed);
    }

    class DisposableProcessor : ITelemetryProcessor, IDisposable
    {
        public void Process(ITelemetry item)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }
}