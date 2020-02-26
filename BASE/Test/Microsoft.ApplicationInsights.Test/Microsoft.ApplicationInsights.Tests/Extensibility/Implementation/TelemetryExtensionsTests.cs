namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class TelemetryExtensionsTests
    {
        class NonSerializableTelemetry : ITelemetry
        {
            public DateTimeOffset Timestamp { get; set; }

            public TelemetryContext Context { get; set; }

            public IExtension Extension { get; set; }
            public string Sequence { get; set; }

            public ITelemetry DeepClone()
            {
                return new NonSerializableTelemetry();
            }

            public void Sanitize()
            {}

            public void SerializeData(ISerializationWriter serializationWriter)
            {}
        }

        [TestMethod]
        public static void CanSetEnvelopeNameForSupportedTypes()
        {
            string testEnvelopeName = "Non_Standard*Envelope.Name";

            var at = new AvailabilityTelemetry();
            var dt = new DependencyTelemetry();
            var et = new EventTelemetry();
            var ext = new ExceptionTelemetry();
            var mt = new MetricTelemetry();
            var pvpt = new PageViewPerformanceTelemetry();
            var pvt = new PageViewTelemetry();
            var rt = new RequestTelemetry();
#pragma warning disable CS0618 // Type or member is obsolete
            var pct = new PerformanceCounterTelemetry();
            var sst = new SessionStateTelemetry();
#pragma warning restore CS0618 // Type or member is obsolete

            at.SetEnvelopeName(testEnvelopeName);
            dt.SetEnvelopeName(testEnvelopeName);
            et.SetEnvelopeName(testEnvelopeName);
            ext.SetEnvelopeName(testEnvelopeName);
            mt.SetEnvelopeName(testEnvelopeName);
            pvpt.SetEnvelopeName(testEnvelopeName);
            pvt.SetEnvelopeName(testEnvelopeName);
            rt.SetEnvelopeName(testEnvelopeName);
            pct.SetEnvelopeName(testEnvelopeName);
            sst.SetEnvelopeName(testEnvelopeName);

            Assert.AreEqual(testEnvelopeName, at.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, dt.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, et.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, ext.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, mt.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, pvpt.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, pvt.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, rt.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, pct.Data.EnvelopeName);
            Assert.AreEqual(testEnvelopeName, sst.Data.EnvelopeName);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public static void SetEnvelopeNameThrowsForUnsupportedTypes()
        {
            var nst = new NonSerializableTelemetry();
            nst.SetEnvelopeName("Any"); // Throws, NonSerializableTelemetry does not implement IAiSerializableTelemetry
        }
    }
}
