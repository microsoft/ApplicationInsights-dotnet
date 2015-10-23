namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    [TestClass]
    public class SamplingPercentageEstimatorSettingsTest
    {
        [TestMethod]
        public void MaxTelemetryItemsPerSecondAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();
            settings.MaxTelemetryItemsPerSecond = -10;

            Assert.True(settings.EffectiveMaxTelemetryItemsPerSecond.EqualsWithPrecision(1E-12, 1E-12));
        }

        [TestMethod]
        public void MinSamplingRateAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();

            settings.MaxSamplingPercentage = 200;
            Assert.Equal(1, settings.EffectiveMinSamplingRate);

            settings.MaxSamplingPercentage = -1;
            Assert.Equal(1E8, settings.EffectiveMinSamplingRate);
        }

        [TestMethod]
        public void MaxSamplingRateAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();

            settings.MinSamplingPercentage = 200;
            Assert.Equal(1, settings.EffectiveMaxSamplingRate);

            settings.MinSamplingPercentage = -1;
            Assert.Equal(1E8, settings.EffectiveMaxSamplingRate);
        }

        [TestMethod]
        public void EvaluationIntervalSecondsAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();
            settings.EvaluationInterval = TimeSpan.Zero;

            Assert.Equal(TimeSpan.FromSeconds(15), settings.EffectiveEvaluationInterval);
        }

        [TestMethod]
        public void SamplingPercentageDecreaseTimeoutSecondsAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();
            settings.SamplingPercentageDecreaseTimeout = TimeSpan.Zero;

            Assert.Equal(TimeSpan.FromMinutes(2), settings.EffectiveSamplingPercentageDecreaseTimeout);
        }

        [TestMethod]
        public void SamplingPercentageIncreaseTimeoutSecondsAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();
            settings.SamplingPercentageIncreaseTimeout = TimeSpan.Zero;

            Assert.Equal(TimeSpan.FromMinutes(15), settings.EffectiveSamplingPercentageIncreaseTimeout);
        }

        [TestMethod]
        public void MovingAverageRatioSecondsAdjustedIfSetToIncorrectValue()
        {
            var settings = new SamplingPercentageEstimatorSettings();
            settings.MovingAverageRatio = -3;

            Assert.True(settings.EffectiveMovingAverageRatio.EqualsWithPrecision(.25, 1E-12));
        }
    }
}
