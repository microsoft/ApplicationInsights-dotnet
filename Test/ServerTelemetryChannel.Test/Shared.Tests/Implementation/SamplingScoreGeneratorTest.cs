namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    
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

            Assert.Equal(eventTelemetrySamplingScore, requestTelemetrySamplingScore, 12);
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

            Assert.Equal(eventTelemetrySamplingScore, requestTelemetrySamplingScore, 12);
        }

        [TestMethod]
        public void SamplingScoreIsRandomIfUserIdAndOperationIdAreNotPresent()
        {
            var eventTelemetry = new EventTelemetry();
            var traceTelemetry = new TraceTelemetry();

            var eventTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(eventTelemetry);
            var traceTelemetrySamplingScore = SamplingScoreGenerator.GetSamplingScore(traceTelemetry);

            Assert.NotEqual(eventTelemetrySamplingScore, traceTelemetrySamplingScore);
        }

        [TestMethod]
        public void StringSamplingHashCodeProducesConsistentValues()
        {
            // we have a predefined set of strings and their hash values
            // the test allows us to make sure we can produce the same hashing
            // results in different versions of sdk
            Dictionary<string, int> stringHash = new Dictionary<string, int>()
                                                 {
                                                     { "ss", 5863819 },
                                                     { "kxi", 193497585 },
                                                     { "wr", 5863950 },
                                                     { "ynehgfhyuiltaiqovbpyhpm", 2139623659 },
                                                     { "iaxxtklcw", 1941943012 },
                                                     { "hjwvqjiiwhoxrtsjma", 1824011880 },
                                                     { "rpiauyg", 1477685542 },
                                                     { "jekvjvh", 691498499 },
                                                     { "hq", 5863454 },
                                                     { "kgqxrftjhefkwlufcxibwjcy", 270215819 },
                                                     { "lkfc", 2090474789 },
                                                     { "skrnpybqqu", 223230949 },
                                                     { "px", 5863725 },
                                                     { "dtn", 193489835 },
                                                     { "nqfcxobaequ", 397313566 },
                                                     { "togxlt", 512267655 },
                                                     { "jvvdkhnahkaujxarkd", 1486894898 },
                                                     { "mcloukvkamiaqja", 56804453 },
                                                     { "ornuu", 270010046 },
                                                     { "otodvlhtvu", 1544494884 },
                                                     { "uhpwhasnvmnykjkitla", 981289895 },
                                                     { "itbnryqnjcgpmfuckghqtg", 1481733713 },
                                                     { "wauetkdnivwlafbfhiedsfx", 2114415420 },
                                                     { "fniwmeidbvd", 508699380 },
                                                     { "vuwdgoxspstvj", 1821547235 },
                                                     { "y", 177694 },
                                                     { "pceqcixfb", 1282453766 },
                                                     { "aentke", 242916867 },
                                                     { "ni", 5863644 },
                                                     { "lbwehevltlnl", 1466602040 },
                                                     { "ymxql", 281700320 },
                                                     { "mvqbaosfuip", 1560556398 },
                                                     { "urmwofajwmmlornynglm", 701710403 },
                                                     { "buptyvonyacerrt", 1315240646 },
                                                     { "cxsqcnyieliatqnwc", 76148095 },
                                                     { "svvco", 274905590 },
                                                     { "luwmjhwyt", 553630912 },
                                                     { "lisvmmug", 822987687 },
                                                     { "mmntilfbmxwuyij", 882214597 },
                                                     { "hqmyv", 261671706 },
                                                 };

            foreach (string input in stringHash.Keys)
            {
                int calculatedHash = input.GetSamplingHashCode();

                Assert.Equal(stringHash[input], calculatedHash);
            }
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
