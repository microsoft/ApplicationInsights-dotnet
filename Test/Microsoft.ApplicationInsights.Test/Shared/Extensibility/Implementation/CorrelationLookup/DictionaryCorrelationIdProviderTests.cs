namespace Microsoft.ApplicationInsights.Extensibility.Implementation.CorrelationLookup
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class DictionaryCorrelationIdProviderTests
    {
        const string testIkey1 = nameof(testIkey1);
        const string testIkey2 = nameof(testIkey2);
        const string testAppId1 = nameof(testAppId1);
        const string testAppId2 = nameof(testAppId2);
        readonly string testCorrelationId1 = CorrelationIdHelper.FormatApplicationId(testAppId1);
        readonly string testCorrelationId2 = CorrelationIdHelper.FormatApplicationId(testAppId2);
        ICorrelationIdProvider correlationIdProvider;

        [TestInitialize]
        public void TestInitialize()
        {
            correlationIdProvider = new DictionaryCorrelationIdProvider()
            {
                DefinedIds = new Dictionary<string, string>
                {
                    {testIkey1, testCorrelationId1 },
                    {testIkey2, testCorrelationId2 }
                }
            };
        }

        [TestMethod]
        public void VerifyLookupsAsExcepected()
        {
            Assert.IsTrue(correlationIdProvider.TryGetCorrelationId(testIkey1, out string actual1));
            Assert.AreEqual(testCorrelationId1, actual1);

            Assert.IsTrue(correlationIdProvider.TryGetCorrelationId(testIkey2, out string actual2));
            Assert.AreEqual(testCorrelationId2, actual2);

            Assert.IsFalse(correlationIdProvider.TryGetCorrelationId("abc", out string actual3));
        }
    }
}
