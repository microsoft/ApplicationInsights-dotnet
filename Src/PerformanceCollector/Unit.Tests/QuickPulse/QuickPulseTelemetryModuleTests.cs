namespace Unit.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryModuleTests
    {
        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                null,
                null,
                null,
                null);

            SetPrivateProperty(module, nameof(module.QuickPulseServiceEndpoint), "https://test.com/api");

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            Assert.IsInstanceOfType(GetPrivateField(module, "serviceClient"), typeof(QuickPulseServiceClient));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromDefault()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                null,
                null,
                null,
                null);

            // ACT
            // do not provide module configuration, force default service client
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            IQuickPulseServiceClient serviceClient = (IQuickPulseServiceClient)GetPrivateField(module, "serviceClient");

            Assert.IsInstanceOfType(serviceClient, typeof(QuickPulseServiceClient));
            Assert.AreEqual(GetPrivateField(module, "serviceUriDefault"), GetPrivateField(serviceClient, "serviceUri"));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleDoesNothingWithoutInstrumentationKey()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(null, telemetryProcessor, serviceClient, performanceCollector, interval, interval);

            module.Initialize(new TelemetryConfiguration());

            // ACT
            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            // ASSERT
            Assert.AreEqual(0, serviceClient.Samples.Count);
            Assert.AreEqual(0, serviceClient.PingCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePingsService()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = false };
            TelemetryConfiguration.Active.InstrumentationKey = "some ikey";

            var module = new QuickPulseTelemetryModule(
                null,
                new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy()),
                serviceClient,
                new PerformanceCollectorMock(),
                interval,
                interval);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            Assert.IsTrue(serviceClient.PingCount > 0);
            Assert.AreEqual(0, serviceClient.Samples.Count);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleCollectsData()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            TelemetryConfiguration.Active.InstrumentationKey = "some ikey";

            var module = new QuickPulseTelemetryModule(null, telemetryProcessor, serviceClient, performanceCollector, interval, interval);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            telemetryProcessor.Process(new RequestTelemetry());
            telemetryProcessor.Process(new DependencyTelemetry());

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            // ASSERT
            Assert.AreEqual(1, serviceClient.PingCount);
            Assert.IsTrue(serviceClient.Samples.Count > 0);

            Assert.IsTrue(serviceClient.Samples.Any(s => s.AIRequestsPerSecond > 0));
            Assert.IsTrue(serviceClient.Samples.Any(s => s.AIDependencyCallsPerSecond > 0));
            Assert.IsTrue(serviceClient.Samples.Any(s => Math.Abs(s.PerfIisRequestsPerSecond) > double.Epsilon));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleTimestampsDataSamples()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            var module = new QuickPulseTelemetryModule(null, telemetryProcessor, serviceClient, performanceCollector, interval, interval);

            var timestampStart = DateTime.UtcNow;

            // ACT
            module.Initialize(new TelemetryConfiguration());

            Thread.Sleep((int)(interval.TotalMilliseconds * 100));

            // ASSERT
            var timestampEnd = DateTime.UtcNow;
            Assert.IsTrue(serviceClient.Samples.All(s => s.StartTimestamp > timestampStart));
            Assert.IsTrue(serviceClient.Samples.All(s => s.StartTimestamp < timestampEnd));
            Assert.IsTrue(serviceClient.Samples.All(s => s.StartTimestamp <= s.EndTimestamp));
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleFetchesTelemetryProcessorFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var configuration = new TelemetryConfiguration();
            var builder = configuration.TelemetryProcessorChainBuilder;
            builder = builder.Use(current => telemetryProcessor);
            builder.Build();

            // ACT
            module.Initialize(configuration);

            // ASSERT
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void QuickPulseTelemetryModuleThrowsWhenNoTelemetryProcessorPresentInConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleManagesTimersCorrectly()
        {
            // ARRANGE
            var pollingInterval = TimeSpan.FromMilliseconds(100);
            var collectionInterval = TimeSpan.FromMilliseconds(10);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = false, ReturnValueFromSubmitSample = true };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            TelemetryConfiguration.Active.InstrumentationKey = "some ikey";

            var module = new QuickPulseTelemetryModule(
                null,
                telemetryProcessor,
                serviceClient,
                performanceCollector,
                pollingInterval,
                collectionInterval);

            // ACT & ASSERT
            module.Initialize(new TelemetryConfiguration());

            // initially, the module is in the polling state
            Thread.Sleep((int)(2.5 * pollingInterval.TotalMilliseconds));

            // 2.5 polling intervals have elapsed, we must have pinged the service twice, but no samples yet
            Assert.AreEqual(2, serviceClient.PingCount);
            Assert.AreEqual(0, serviceClient.Samples.Count);

            serviceClient.Reset();

            // now the service wants the data
            serviceClient.ReturnValueFromPing = true;
            serviceClient.ReturnValueFromSubmitSample = true;

            Thread.Sleep((int)(30 * collectionInterval.TotalMilliseconds));

            // 30  collection intervals have elapsed, we must have pinged the service once, and then started sending samples
            Assert.AreEqual(1, serviceClient.PingCount);
            Assert.IsTrue(serviceClient.Samples.Count > 0);

            serviceClient.Reset();

            // the service doesn't want the data anymore
            serviceClient.ReturnValueFromPing = false;
            serviceClient.ReturnValueFromSubmitSample = false;

            Thread.Sleep((int)(2.5 * pollingInterval.TotalMilliseconds));

            // 2.5 polling intervals have elapsed, we must have submitted one sample, stopped collecting and pinged the service twice afterwards
            Assert.AreEqual(1, serviceClient.Samples.Count);
            Assert.AreEqual(2, serviceClient.PingCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePingBacksOff()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleSubmitBacksOff()
        {
            Assert.Fail();
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleResendsFailedSamples()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock { ReturnValueFromPing = true, ReturnValueFromSubmitSample = null };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            TelemetryConfiguration.Active.InstrumentationKey = "some ikey";

            var module = new QuickPulseTelemetryModule(null, telemetryProcessor, serviceClient, performanceCollector, interval, interval);

            module.Initialize(new TelemetryConfiguration());

            // ACT
            // below timeout should be sufficient for the module to get to maximum storage capacity
            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            // ASSERT
            Assert.AreEqual(10, serviceClient.LastSampleBatchSize);
        }

        #region Helpers
        private static void SetPrivateProperty(object obj, string propertyName, string propertyValue)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
            propertyInfo.GetSetMethod(true).Invoke(obj, new object[] { propertyValue });
        }

        private static object GetPrivateField(object obj, string fieldName)
        {
            FieldInfo fieldInfo = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return fieldInfo.GetValue(obj);
        }
        #endregion
    }
}