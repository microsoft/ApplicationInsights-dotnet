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

            bool? value = null;
            config.EvaluateExperimentalFeature("abc", ref value);

            Assert.IsFalse(value.Value);
        }

        [TestMethod]
        public void VerifyExperimentalFeaturesExtension()
        {
            var config = new TelemetryConfiguration
            {
                ExperimentalFeatures = new string[] { "abc" }
            };

            bool? value = null;
            config.EvaluateExperimentalFeature("abc", ref value);

            Assert.IsTrue(value.Value);


            bool? fakeValue = null;
            config.EvaluateExperimentalFeature("fake", ref fakeValue);

            Assert.IsFalse(fakeValue.Value);
        }
    }
}
