using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Experimental
{
    [TestClass]
    public class ExperimentalFeaturesTests
    {
        [TestMethod]
        public void VerifyExperimentalFeaturesExtensionIfConfigEmpty()
        {
            var config = new TelemetryConfiguration();

            bool value = config.EvaluateExperimentalFeature("abc");
            Assert.IsFalse(value);
        }

        [TestMethod]
        public void VerifyExperimentalFeaturesExtension()
        {
            var config = new TelemetryConfiguration
            {
                ExperimentalFeatures = { "abc" }
            };

            bool value = config.EvaluateExperimentalFeature("abc");
            Assert.IsTrue(value);

            bool fakeValue = config.EvaluateExperimentalFeature("fake");
            Assert.IsFalse(fakeValue);
        }
    }
}
