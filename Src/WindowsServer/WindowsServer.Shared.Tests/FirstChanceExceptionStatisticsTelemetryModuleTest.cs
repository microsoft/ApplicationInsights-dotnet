namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.ExceptionServices;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestFramework;
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
        public void FirstChanceExceptionStatisticsTelemetryModuleDoNotCauseStackOverflow()
        {
            this.configuraiton.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    throw new Exception("this exception may cause stack overflow");
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuraiton);

                try
                {
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    Assert.Equal("test", exc.Message);
                }
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleTracksMetricWithTypeAndMethodOnException()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuraiton.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuraiton);

                try
                {
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    Assert.Equal("test", exc.Message);
                }
            }

            Assert.Equal(1, metrics.Count);
            Assert.Equal("Exceptions Thrown", metrics[0].Key.Name);

            var dims = metrics[0].Key.Dimensions;
            Assert.Equal(2, dims.Count);

            Assert.True(dims.Contains(new KeyValuePair<string, string>("type", typeof(Exception).FullName)));
            Assert.True(dims.Contains(new KeyValuePair<string, string>("method", typeof(FirstChanceExceptionStatisticsTelemetryModuleTest).FullName + ".FirstChanceExceptionStatisticsTelemetryModuleTracksMetricWithTypeAndMethodOnException")));
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleUsesOperationNameAsDimension()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuraiton.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            this.configuraiton.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName";
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuraiton);

                try
                {
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    Assert.Equal("test", exc.Message);
                }
            }

            Assert.Equal(1, metrics.Count);
            Assert.Equal("Exceptions Thrown", metrics[0].Key.Name);

            var dims = metrics[0].Key.Dimensions;
            Assert.Equal(3, dims.Count);

            Assert.True(dims.Contains(new KeyValuePair<string, string>("operation", "operationName")));
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
