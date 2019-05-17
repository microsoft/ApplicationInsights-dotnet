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
                ExperimentalFeatures = new string[] { "abc" }
            };

            bool value = config.EvaluateExperimentalFeature("abc");
            Assert.IsTrue(value);

            bool fakeValue = config.EvaluateExperimentalFeature("fake");
            Assert.IsFalse(fakeValue);
        }

        [TestMethod]
        public void VerifyExperimentalFeaturesClass()
        {
            // test 1: feature does not exist
            var config1 = new TelemetryConfiguration
            {
                ExperimentalFeatures = new string[] { "abc" }
            };

            var value1 = ExperimentalFeatures.IsExampleFeatureEnabled(config1);
            Assert.IsFalse(value1);

            // test2: feature does exist
            var config2 = new TelemetryConfiguration
            {
                ExperimentalFeatures = new string[] { "exampleFeature" }
            };

            var value2 = ExperimentalFeatures.IsExampleFeatureEnabled(config2);
            Assert.IsTrue(value2);
        }
    }
}
