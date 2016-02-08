namespace Unit.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryModuleTests
    {
        [TestMethod]
        public void QuickPulseTelemetryModuleInitializesServiceClientFromConfiguration()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule();

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
            var module = new QuickPulseTelemetryModule();

            // ACT
            // do not provide module configuration, force default service client
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            IQuickPulseServiceClient serviceClient = (IQuickPulseServiceClient)GetPrivateField(module, "serviceClient");

            Assert.IsInstanceOfType(serviceClient, typeof(QuickPulseServiceClient));
            Assert.AreEqual(GetPrivateField(module, "serviceUriDefault"), GetPrivateField(serviceClient, "serviceUri"));
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePlugsInTelemetryInitializerCorrectly()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule();
            var configuration = TelemetryConfiguration.CreateDefault();

            // ACT
            module.Initialize(configuration);

            // ASSERT
            Assert.IsInstanceOfType(configuration.TelemetryInitializers.Last(), typeof(IQuickPulseTelemetryInitializer));
        }

        [TestMethod]
        public void QuickPulseTelemetryModulePingsServiceWhenIdle()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock()
                                    {
                                        ReturnValueFromPing = false,
                                        ReturnValueFromSubmitSample = false
                                    };
            var module = new QuickPulseTelemetryModule(
                null,
                null,
                serviceClient,
                new PerformanceCollectorMock(),
                interval,
                interval);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            // ASSERT
            Thread.Sleep(interval.Milliseconds * 100);

            Assert.IsTrue(serviceClient.PingCount > 0);
            Assert.AreEqual(0, serviceClient.SampleCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryModuleCollectsData()
        {
            // ARRANGE
            var interval = TimeSpan.FromMilliseconds(1);
            var serviceClient = new QuickPulseServiceClientMock()
                                    {
                                        ReturnValueFromPing = true,
                                        ReturnValueFromSubmitSample = true
                                    };
            var performanceCollector = new PerformanceCollectorMock();
            var telemetryInitializer = new QuickPulseTelemetryInitializer();

            var module = new QuickPulseTelemetryModule(
                null,
                telemetryInitializer,
                serviceClient,
                performanceCollector,
                interval,
                interval);

            // ACT
            module.Initialize(new TelemetryConfiguration());

            Thread.Sleep(interval.Milliseconds * 100);

            telemetryInitializer.Initialize(new RequestTelemetry());
            telemetryInitializer.Initialize(new DependencyTelemetry());

            Thread.Sleep(interval.Milliseconds * 100);

            // ASSERT
            Assert.AreEqual(1, serviceClient.PingCount);
            Assert.IsTrue(serviceClient.SampleCount > 0);

            Assert.IsTrue(serviceClient.Samples.Any(s => s.AIRequestsPerSecond > 0));
            Assert.IsTrue(serviceClient.Samples.Any(s => s.AIDependencyCallsPerSecond > 0));
            Assert.IsTrue(serviceClient.Samples.Any(s => Math.Abs(s.PerfIisRequestsPerSecond) > double.Epsilon));
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