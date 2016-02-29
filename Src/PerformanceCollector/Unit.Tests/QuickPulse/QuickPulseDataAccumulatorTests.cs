namespace Unit.Tests
{
    using System;

    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    
    [TestClass]
    public class QuickPulseDataAccumulatorTests
    {
        private const long MaxCount = 524287;
        private const long MaxDuration = 17592186044415;

        [TestMethod]
        public void QuickPulseDataAccumulatorEncodesCountAndDuration()
        {
            // ARRANGE
            long count = 42;
            long duration = 102;

            // ACT
            long encodedValue = QuickPulseDataAccumulator.EncodeCountAndDuration(count, duration);
            Tuple<long, long> decodedValues = QuickPulseDataAccumulator.DecodeCountAndDuration(encodedValue);

            // ASSERT
            Assert.AreEqual(count, decodedValues.Item1);
            Assert.AreEqual(duration, decodedValues.Item2);
        }

        [TestMethod]
        public void QuickPulseDataAccumulatorEncodesCountAndDurationMaxValues()
        {
            // ARRANGE
            
            // ACT
            long encodedValue = QuickPulseDataAccumulator.EncodeCountAndDuration(MaxCount, MaxDuration);
            Tuple<long, long> decodedValues = QuickPulseDataAccumulator.DecodeCountAndDuration(encodedValue);

            // ASSERT
            Assert.AreEqual(MaxCount, decodedValues.Item1);
            Assert.AreEqual(MaxDuration, decodedValues.Item2);
        }

        [TestMethod]
        public void QuickPulseDataAccumulatorEncodesCountAndDurationOverflowCount()
        {
            // ARRANGE
         
            // ACTt
            long encodedValue = QuickPulseDataAccumulator.EncodeCountAndDuration(MaxCount + 1, MaxDuration);
            Tuple<long, long> decodedValues = QuickPulseDataAccumulator.DecodeCountAndDuration(encodedValue);

            // ASSERT
            Assert.AreEqual(0, decodedValues.Item1);
            Assert.AreEqual(0, decodedValues.Item2);
        }

        [TestMethod]
        public void QuickPulseDataAccumulatorEncodesCountAndDurationOverflowDuration()
        {
            // ARRANGE

            // ACTt
            long encodedValue = QuickPulseDataAccumulator.EncodeCountAndDuration(MaxCount, MaxDuration + 1);
            Tuple<long, long> decodedValues = QuickPulseDataAccumulator.DecodeCountAndDuration(encodedValue);

            // ASSERT
            Assert.AreEqual(0, decodedValues.Item1);
            Assert.AreEqual(0, decodedValues.Item2);
        }
    }
}
