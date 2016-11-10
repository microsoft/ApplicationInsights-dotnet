namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class FirstChanceExceptionStatisticsTelemetryModuleTest : IDisposable
    {
        private TelemetryConfiguration configuraiton;
        private IList<ITelemetry> items;

        [TestInitialize]
        public void TestInitialize()
        {
            this.items = new List<ITelemetry>();

            this.configuraiton = new TelemetryConfiguration();

            this.configuraiton.TelemetryChannel = new StubTelemetryChannel
            {
                OnSend = telemetry => this.items.Add(telemetry),
                EndpointAddress = "http://test.com"
            };
            this.configuraiton.InstrumentationKey = "MyKey";
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.configuraiton = null;
            this.items.Clear();
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleDoNotThrowOnNullException()
        {
            EventHandler<FirstChanceExceptionEventArgs> handler = null;
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule(
                h => handler = h,
                _ => { }))
            {
                module.Initialize(this.configuraiton);
                handler.Invoke(null, new FirstChanceExceptionEventArgs(null));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleRecievesFirstChance()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuraiton);

                try
                {
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    Assert.NotNull(exc);
                }
            }
        }

        [TestMethod]
        public void ModuleConstructorCallsRegister()
        {
            EventHandler<FirstChanceExceptionEventArgs> handler = null;
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule(
                h => handler = h,
                _ => { }))
            {
                module.Initialize(this.configuraiton);
            }

            Assert.NotNull(handler);
        }

        [TestMethod]
        public void DisposeCallsUnregister()
        {
            EventHandler<FirstChanceExceptionEventArgs> handler = null;
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule(
                _ => { },
                h => handler = h))
            {
                module.Initialize(this.configuraiton);
            }

            Assert.NotNull(handler);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.configuraiton != null)
                {
                    this.configuraiton.Dispose();
                }
            }
        }
    }
}
