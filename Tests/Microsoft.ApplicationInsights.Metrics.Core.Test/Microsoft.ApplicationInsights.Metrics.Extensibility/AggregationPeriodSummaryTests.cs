using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;

using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    [TestClass]
    public class AggregationPeriodSummaryTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            {
                var period = new AggregationPeriodSummary(null, null);
                Assert.IsNotNull(period);
            }
        }

        /// <summary />
        [TestMethod]
        public void PersistentAggregates()
        {
            {
                ITelemetry[] p = new ITelemetry[0];
                var period = new AggregationPeriodSummary(p, null);

                Assert.IsNull(period.NonpersistentAggregates);

                Assert.IsNotNull(period.PersistentAggregates);
                Assert.AreSame(p, period.PersistentAggregates);
                Assert.AreEqual(0, period.PersistentAggregates.Count);
            }
            {
                ITelemetry[] p = new ITelemetry[] { new MetricTelemetry("MT1", 1), new MetricTelemetry("MT2", 2), new MetricTelemetry("MT3", 3) };
                var period = new AggregationPeriodSummary(p, null);

                Assert.IsNull(period.NonpersistentAggregates);

                Assert.IsNotNull(period.PersistentAggregates);
                Assert.AreSame(p, period.PersistentAggregates);
                Assert.AreEqual(3, period.PersistentAggregates.Count);

                Assert.IsTrue(period.PersistentAggregates[0] is MetricTelemetry);
                Assert.AreEqual("MT1", (period.PersistentAggregates[0] as MetricTelemetry).Name);
                Assert.AreEqual(1, (period.PersistentAggregates[0] as MetricTelemetry).Sum);

                Assert.IsTrue(period.PersistentAggregates[1] is MetricTelemetry);
                Assert.AreEqual("MT2", (period.PersistentAggregates[1] as MetricTelemetry).Name);
                Assert.AreEqual(2, (period.PersistentAggregates[1] as MetricTelemetry).Sum);

                Assert.IsTrue(period.PersistentAggregates[2] is MetricTelemetry);
                Assert.AreEqual("MT3", (period.PersistentAggregates[2] as MetricTelemetry).Name);
                Assert.AreEqual(3, (period.PersistentAggregates[2] as MetricTelemetry).Sum);
            }
            {
                ITelemetry[] np = new ITelemetry[] { new MetricTelemetry("MT1", 1), new MetricTelemetry("MT2", 2), new MetricTelemetry("MT3", 3) };
                var period = new AggregationPeriodSummary(null, np);

                Assert.IsNull(period.PersistentAggregates);

                Assert.IsNotNull(period.NonpersistentAggregates);
                Assert.AreSame(np, period.NonpersistentAggregates);
                Assert.AreEqual(3, period.NonpersistentAggregates.Count);

                Assert.IsTrue(period.NonpersistentAggregates[0] is MetricTelemetry);
                Assert.AreEqual("MT1", (period.NonpersistentAggregates[0] as MetricTelemetry).Name);
                Assert.AreEqual(1, (period.NonpersistentAggregates[0] as MetricTelemetry).Sum);

                Assert.IsTrue(period.NonpersistentAggregates[1] is MetricTelemetry);
                Assert.AreEqual("MT2", (period.NonpersistentAggregates[1] as MetricTelemetry).Name);
                Assert.AreEqual(2, (period.NonpersistentAggregates[1] as MetricTelemetry).Sum);

                Assert.IsTrue(period.NonpersistentAggregates[2] is MetricTelemetry);
                Assert.AreEqual("MT3", (period.NonpersistentAggregates[2] as MetricTelemetry).Name);
                Assert.AreEqual(3, (period.NonpersistentAggregates[2] as MetricTelemetry).Sum);
            }

        }
    }
}
