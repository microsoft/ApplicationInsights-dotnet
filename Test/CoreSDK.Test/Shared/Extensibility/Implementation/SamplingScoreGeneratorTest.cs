namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Globalization;

    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using EndpointSessionContext = Microsoft.Developer.Analytics.DataCollection.Model.v2.SessionContextData;

    [TestClass]
    public class SamplingScoreGeneratorTest
    {
        private static readonly Random Rand = new Random();

        [TestMethod]
        public void SamplingScoreGeneratedUsingUserIdIfPresent()
        {
            string userId = GenerateRandomUserId();

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Context.User.Id = userId;
            eventTelemetry.Context.Operation.Id = GenerateRandomOperaitonId();

            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.User.Id = userId;
            requestTelemetry.Context.Operation.Id = GenerateRandomOperaitonId();

            var eventTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(eventTelemetry);
            var requestTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(requestTelemetry);

            Assert.True(eventTelemetrySamplingScore.EqualsWithPrecision(requestTelemetrySamplingScore, 1.0E-12));
        }

        [TestMethod]
        public void SamplingScoreGeneratedUsingOperationIdIfPresent()
        {
            string operationId = GenerateRandomOperaitonId();

            var eventTelemetry = new EventTelemetry();
            eventTelemetry.Context.Operation.Id = operationId;

            var requestTelemetry = new RequestTelemetry();
            requestTelemetry.Context.Operation.Id = operationId;

            var eventTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(eventTelemetry);
            var requestTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(requestTelemetry);

            Assert.True(eventTelemetrySamplingScore.EqualsWithPrecision(requestTelemetrySamplingScore, 1.0E-12));
        }

        [TestMethod]
        public void SamplingScoreIsRandomIfUserIdAndOperationIdAreNotPresent()
        {
            var eventTelemetry = new EventTelemetry();
            var requestTelemetry = new RequestTelemetry();

            var eventTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(eventTelemetry);
            var requestTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(requestTelemetry);

            Assert.False(eventTelemetrySamplingScore.EqualsWithPrecision(requestTelemetrySamplingScore, 1.0E-12));
        }

        private static string GenerateRandomUserId()
        {
            var userIdLength = Rand.Next(3, 12);

            string userId = string.Empty;

            for (int i = 0; i < userIdLength; i++)
            {
                userId += (char)('a' + Rand.Next(0, 25));
            }

            return userId;
        }

        private static string GenerateRandomOperaitonId()
        {
            return WeakConcurrentRandom.Instance.Next().ToString(CultureInfo.InvariantCulture);
        }
    }
}
