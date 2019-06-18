namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Sampling
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class SamplingRateStoreTest
    {
        [TestMethod]
        public void StoreIsInitializedWith100Rate()
        {
            var store = new SamplingRateStore();
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request), 100);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency), 100);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event), 100);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception), 100);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message), 100);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView), 100);
        }

        [TestMethod]
        public void StoreAllowsToSetTheRatePerType()
        {
            var store = new SamplingRateStore();
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request, 10);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency, 11);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event, 12);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception, 13);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message, 14);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView, 15);

            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request), 10);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency), 11);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event), 12);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception), 13);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message), 14);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView), 15);
        }

        [TestMethod]
        public void StoreAllowsToOverrideTheRatePerType()
        {
            var store = new SamplingRateStore();
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request, 10);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency, 11);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event, 12);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception, 13);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message, 14);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView, 15);

            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request, 1);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency, 2);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event, 3);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception, 4);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message, 5);
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView, 6);

            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request), 1);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.RemoteDependency), 2);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Event), 3);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Exception), 4);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Message), 5);
            Assert.AreEqual(store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.PageView), 6);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsOnSavingUnsupportedType()
        {
            var store = new SamplingRateStore();
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Metric, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsOnSavingUnknownType()
        {
            var store = new SamplingRateStore();
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.None, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsOnSavingMultiTypedItem()
        {
            var store = new SamplingRateStore();
            store.SetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request | SamplingTelemetryItemTypes.RemoteDependency, 100);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsForUnsupportedTypes()
        {
            var store = new SamplingRateStore();
            store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Metric);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsForUnknownType()
        {
            var store = new SamplingRateStore();
            store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.None);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void StoreThrowsForMultiTypeGet()
        {
            var store = new SamplingRateStore();
            store.GetLastObservedSamplingPercentage(SamplingTelemetryItemTypes.Request | SamplingTelemetryItemTypes.RemoteDependency);
        }
    }
}
