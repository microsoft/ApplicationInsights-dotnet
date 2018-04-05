using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Metrics.Extensibility;
using Microsoft.ApplicationInsights.Metrics.TestUtility;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MetricConfigurationTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            { 
                var config = new MetricConfiguration(
                                    seriesCountLimit:           1000,
                                    valuesPerDimensionLimit:    100,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));
                Assert.IsNotNull(config);
                Assert.AreEqual(1000, config.SeriesCountLimit);
                Assert.AreEqual(100, config.ValuesPerDimensionLimit);
                Assert.IsNotNull(config.SeriesConfig);
                Assert.IsInstanceOfType(config.SeriesConfig, typeof(MetricSeriesConfigurationForMeasurement));
                Assert.AreEqual(false, config.SeriesConfig.RequiresPersistentAggregation);
                Assert.AreEqual(false, ((MetricSeriesConfigurationForMeasurement) config.SeriesConfig).RestrictToUInt32Values);
            }
            { 
                var config = new MetricConfiguration(
                                    seriesCountLimit:           1,
                                    valuesPerDimensionLimit:    0,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: true));
                Assert.IsNotNull(config);
                Assert.AreEqual(1, config.SeriesCountLimit);
                Assert.AreEqual(0, config.ValuesPerDimensionLimit);
                Assert.IsNotNull(config.SeriesConfig);
                Assert.IsInstanceOfType(config.SeriesConfig, typeof(MetricSeriesConfigurationForMeasurement));
                Assert.AreEqual(false, config.SeriesConfig.RequiresPersistentAggregation);
                Assert.AreEqual(true, ((MetricSeriesConfigurationForMeasurement) config.SeriesConfig).RestrictToUInt32Values);
            }
            { 
                var config = new MetricConfiguration(
                                    seriesCountLimit:           Int32.MaxValue,
                                    valuesPerDimensionLimit:    Int32.MaxValue,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));
                Assert.IsNotNull(config);
                Assert.AreEqual(Int32.MaxValue, config.SeriesCountLimit);
                Assert.AreEqual(Int32.MaxValue, config.ValuesPerDimensionLimit);
                Assert.IsNotNull(config.SeriesConfig);
                Assert.IsInstanceOfType(config.SeriesConfig, typeof(MetricSeriesConfigurationForMeasurement));
                Assert.AreEqual(false, config.SeriesConfig.RequiresPersistentAggregation);
                Assert.AreEqual(false, ((MetricSeriesConfigurationForMeasurement) config.SeriesConfig).RestrictToUInt32Values);
            }
            { 
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => new MetricConfiguration(
                                    seriesCountLimit:           0,
                                    valuesPerDimensionLimit:    100,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false)) );
            }
            { 
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => new MetricConfiguration(
                                    seriesCountLimit:           -1,
                                    valuesPerDimensionLimit:    100,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false)) );
            }
            { 
                Assert.ThrowsException<ArgumentOutOfRangeException>( () => new MetricConfiguration(
                                    seriesCountLimit:           1000,
                                    valuesPerDimensionLimit:    -1,
                                    seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false)) );
            }
            {
                Assert.ThrowsException<ArgumentNullException>(() => new MetricConfiguration(
                                    seriesCountLimit:           1000,
                                    valuesPerDimensionLimit:    100,
                                    seriesConfig:               null) );
            }
        }

        /// <summary />
        [TestMethod]
        public void SeriesCountLimit()
        {
            // Covered in this test:
            Ctor();
        }

        /// <summary />
        [TestMethod]
        public void ValuesPerDimensionLimit()
        {
            // Covered in this test:
            Ctor();
        }

        /// <summary />
        [TestMethod]
        public void SeriesConfig()
        {
            // Covered in this test:
            Ctor();
        }

        /// <summary />
        [TestMethod]
        public void TestEquals()
        {
            var config1 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.IsTrue(config1.Equals(config1));

            var config2 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.IsTrue(config1.Equals(config2));
            Assert.IsTrue(config2.Equals(config1));

            config2 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: true));

            Assert.IsFalse(config1.Equals(config2));
            Assert.IsFalse(config2.Equals(config1));

            config2 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    101,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.IsFalse(config1.Equals(config2));
            Assert.IsFalse(config2.Equals(config1));

            config2 = new MetricConfiguration(
                                seriesCountLimit:           1001,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.IsFalse(config1.Equals(config2));
            Assert.IsFalse(config2.Equals(config1));
        }

        /// <summary />
        [TestMethod]
        public void TestGetHashCode()
        {
            var config1 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.AreNotEqual(0, config1.GetHashCode());

            var config2 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.AreEqual(config1.GetHashCode(), config2.GetHashCode());

            config2 = new MetricConfiguration(
                                seriesCountLimit:           1000,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: true));

            Assert.AreNotEqual(config1.GetHashCode(), config2.GetHashCode());
            Assert.AreNotEqual(0, config2.GetHashCode());

            config2 = new MetricConfiguration(
                                seriesCountLimit:           1001,
                                valuesPerDimensionLimit:    100,
                                seriesConfig:               new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false));

            Assert.AreNotEqual(config1.GetHashCode(), config2.GetHashCode());
            Assert.AreNotEqual(0, config2.GetHashCode());
        }
    }
}
