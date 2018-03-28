namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class StaticCorrelationIdProviderTests
    {
        [TestMethod]
        public void VerifySameCorrelationIdForDifferentKeys()
        {
            string testIkey1 = nameof(testIkey1);
            string testIkey2 = nameof(testIkey2);
            string testAppId = nameof(testAppId);
            string testCorrelationId = CorrelationIdHelper.FormatApplicationId(testAppId);

            var correlationIdProvider = new StaticCorrelationIdProvider()
            {
                CorrelationId = testCorrelationId
            };

            Assert.IsTrue(correlationIdProvider.TryGetCorrelationId(testIkey1, out string actual1));
            Assert.AreEqual(testCorrelationId, actual1);
            Assert.IsTrue(correlationIdProvider.TryGetCorrelationId(testIkey2, out string actual2));
            Assert.AreEqual(testCorrelationId, actual2);
        }
    }
}
