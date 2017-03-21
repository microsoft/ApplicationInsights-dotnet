namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.ExceptionServices;
    using DataContracts;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Web.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestFramework;
    using Assert = Xunit.Assert;

    [TestClass]
    public class FirstChanceExceptionStatisticsTelemetryModuleTest : IDisposable
    {
        private TelemetryConfiguration configuration;
        private IList<ITelemetry> items;

        [TestInitialize]
        public void TestInitialize()
        {
            this.items = new List<ITelemetry>();

            this.configuration = new TelemetryConfiguration();

            this.configuration.TelemetryChannel = new StubTelemetryChannel
            {
                OnSend = telemetry => this.items.Add(telemetry),
                EndpointAddress = "http://test.com"
            };
            this.configuration.InstrumentationKey = "MyKey";
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.configuration = null;
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
                module.Initialize(this.configuration);
                handler.Invoke(null, new FirstChanceExceptionEventArgs(null));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleDoNotCauseStackOverflow()
        {
            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    throw new Exception("this exception may cause stack overflow as will be thrown during the processing of another exception");
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                try
                {
                    // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    // make sure it is the same exception as was initially thrown
                    Assert.Equal("test", exc.Message);
                }
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleTracksMetricWithTypeAndMethodOnException()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                try
                {
                    // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    // code to prevent compiler optimizations
                    Assert.Equal("test", exc.Message);
                }
            }

            Assert.Equal(1, metrics.Count);
            Assert.Equal("Exceptions Thrown", metrics[0].Key.Name);

            var dims = metrics[0].Key.Dimensions;
            Assert.Equal(2, dims.Count);

            Assert.True(dims.Contains(new KeyValuePair<string, string>("type", typeof(Exception).FullName)));
            string value;
            Assert.True(dims.TryGetValue("method", out value));
            Assert.True(value.StartsWith(typeof(FirstChanceExceptionStatisticsTelemetryModuleTest).FullName + "." + nameof(this.FirstChanceExceptionStatisticsTelemetryModuleTracksMetricWithTypeAndMethodOnException), StringComparison.Ordinal));
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleUsesOperationNameAsDimension()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName";
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                try
                {
                    // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    // code to prevent profiler optimizations
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
        public void FirstChanceExceptionStatisticsTelemetryModuleMarksOperationAsInternal()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                try
                {
                    SdkInternalOperationsMonitor.Enter();

                    // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                    throw new Exception("test");
                }
                catch (Exception exc)
                {
                    // code to prevent profiler optimizations
                    Assert.Equal("test", exc.Message);
                }
                finally
                {
                    SdkInternalOperationsMonitor.Exit();
                }
            }

            Assert.Equal(1, metrics.Count);
            Assert.Equal("Exceptions Thrown", metrics[0].Key.Name);

            var dims = metrics[0].Key.Dimensions;
            Assert.Equal(3, dims.Count);

            string operationName;
            Assert.True(dims.TryGetValue("operation", out operationName));
            Assert.Equal("AI (Internal)", operationName);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWillDimCapOperationName()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            int operationId = 0;

            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName " + (operationId++);
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                for (int i = 0; i < 200; i++)
                {
                    try
                    {
                        // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                        throw new Exception("test");
                    }
                    catch (Exception exc)
                    {
                        // code to prevent profiler optimizations
                        Assert.Equal("test", exc.Message);
                    }
                }
            }

            Assert.Equal(200, metrics.Count);
            Assert.Equal(102, this.items.Count);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWillNotDimCapTheSameOperationName()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName";
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                for (int i = 0; i < 200; i++)
                {
                    try
                    {
                        // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                        throw new Exception("test");
                    }
                    catch (Exception exc)
                    {
                        // code to prevent profiler optimizations
                        Assert.Equal("test", exc.Message);
                    }
                }
            }

            Assert.Equal(200, metrics.Count);
            Assert.Equal(1, this.items.Count);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWillDimCapAfterCacheTimeout()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            int operationId = 0;

            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName " + (operationId++);
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                module.DimCapTimeout = DateTime.UtcNow.Ticks - 1;

                for (int i = 0; i < 200; i++)
                {
                    if (i == 101)
                    {
                        module.DimCapTimeout = DateTime.UtcNow.Ticks - 1;
                    }

                    try
                    {
                        // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                        throw new Exception("test");
                    }
                    catch (Exception exc)
                    {
                        // code to prevent profiler optimizations
                        Assert.Equal("test", exc.Message);
                    }
                }
            }

            Assert.Equal(200, metrics.Count);
            Assert.Equal(200, this.items.Count);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryExceptionsAreThrottled()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            this.configuration.TelemetryInitializers.Add(new StubTelemetryInitializer()
            {
                OnInitialize = (item) =>
                {
                    item.Context.Operation.Name = "operationName";
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                module.TargetMovingAverage = 50;

                for (int i = 0; i < 200; i++)
                {
                    try
                    {
                        // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                        throw new Exception("test");
                    }
                    catch (Exception exc)
                    {
                        // code to prevent profiler optimizations
                        Assert.Equal("test", exc.Message);
                    }
                }
            }

            int countProcessed = 0;
            int countThrottled = 0;

            foreach (KeyValuePair<Metric, double> items in metrics)
            {
                if (items.Key.Dimensions.Count == 1)
                {
                    countThrottled++;
                }
                else
                {
                    countProcessed++;
                }
            }

            // The test starts with the current moving average being 0. With the setting of the
            // weight for the new sample being .3 and the target moving average being 50 (as
            // set in the test), this means 50 / .3 = 166 becomes the throttle limit for this window.
            Assert.Equal(166, countProcessed);
            Assert.Equal(34, countThrottled);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleDoNotIncrementOnRethrow()
        {
            var metrics = new List<KeyValuePair<Metric, double>>();
            this.configuration.MetricProcessors.Add(new StubMetricProcessor()
            {
                OnTrack = (m, v) =>
                {
                    metrics.Add(new KeyValuePair<Metric, double>(m, v));
                }
            });

            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                try
                {
                    try
                    {
                        // FirstChanceExceptionStatisticsTelemetryModule will process this exception
                        throw new Exception("test");
                    }
                    catch (Exception ex)
                    {
                        // this assert is neede to avoid code optimization
                        Assert.Equal("test", ex.Message);
                        throw;
                    }
                }
                catch (Exception exc)
                {
                    Assert.Equal("test", exc.Message);
                }
            }

            Assert.Equal(2, metrics.Count);
            Assert.Equal("Exceptions Thrown", metrics[0].Key.Name);

            Assert.Equal(1, metrics[0].Value, 15);
            Assert.Equal(0, metrics[1].Value, 15);

            Assert.Equal(2, this.items.Count);

            Assert.Equal(1, ((MetricTelemetry) this.items[0]).Count);
            Assert.Equal(1, ((MetricTelemetry) this.items[1]).Count);

            // One of them should be 0 as re-thorwn, another - one
            Assert.Equal(0, Math.Min(((MetricTelemetry) this.items[0]).Sum, ((MetricTelemetry) this.items[1]).Sum), 15);
            Assert.Equal(1, Math.Max(((MetricTelemetry) this.items[0]).Sum, ((MetricTelemetry) this.items[1]).Sum), 15);
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWasTrackedReturnsTrueForTheSameException()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new Exception();

                Assert.False(module.WasExceptionTracked(exception));
                Assert.True(module.WasExceptionTracked(exception));
                Assert.True(module.WasExceptionTracked(exception));
                Assert.True(module.WasExceptionTracked(exception));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWasTrackedReturnsTrueForInnerException()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new Exception();

                Assert.False(module.WasExceptionTracked(exception));

                var wrapper = new Exception("wrapper", exception);

                Assert.True(module.WasExceptionTracked(wrapper));
                Assert.True(module.WasExceptionTracked(wrapper));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWasTrackedReturnsFalseForInnerExceptionTwoLevelsUp()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new Exception();

                Assert.False(module.WasExceptionTracked(exception));

                var wrapper1 = new Exception("wrapper 1", exception);
                var wrapper2 = new Exception("wrapper 2", wrapper1);

                Assert.False(module.WasExceptionTracked(wrapper2));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWasTrackedReturnsTrueForAggExc()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new Exception();

                Assert.False(module.WasExceptionTracked(exception));

                var aggExc = new AggregateException(exception);
                Assert.True(module.WasExceptionTracked(aggExc));
            }
        }

        [TestMethod]
        public void FirstChanceExceptionStatisticsTelemetryModuleWasTrackedReturnsFalseForAggExcWithNotTrackedInnerExceptions()
        {
            using (var module = new FirstChanceExceptionStatisticsTelemetryModule())
            {
                module.Initialize(this.configuration);

                var exception = new Exception();

                var aggExc = new AggregateException(exception);
                Assert.False(module.WasExceptionTracked(aggExc));
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
                module.Initialize(this.configuration);
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
                module.Initialize(this.configuration);
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
                if (this.configuration != null)
                {
                    this.configuration.Dispose();
                }
            }
        }
    }
}
