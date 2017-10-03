using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricAggregationManagerTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            var manager = new MetricAggregationManager();
            Assert.IsNotNull(manager);
        }

        /// <summary />
        [TestMethod]
        public void AddAndCount()
        {
            var manager = new MetricAggregationManager();

            DateTimeOffset dto = new DateTimeOffset(2017, 10, 2, 17, 5, 0, TimeSpan.FromHours(-7));

            manager.StartOrCycleAggregators(MetricAggregationCycleKind.Custom, dto, futureFilter: null);
        }

        
    }
}
