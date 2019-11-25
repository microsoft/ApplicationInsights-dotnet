namespace Microsoft.ApplicationInsights
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class W3CUtilitiesTests
    {
        [TestMethod]
        public void ParseCompatibleTraceIdParent()
        {
            Assert.IsTrue(W3C.Internal.W3CUtilities.TryGetTraceId("|0123456789abcdef0123456789abcdef.1.2.3.54.", out var traceId));
            Assert.AreEqual("0123456789abcdef0123456789abcdef", traceId.ToString());
        }

        [TestMethod]
        public void ParseIncompatibleTraceIdParent_Not32HexChars()
        {
            Assert.IsFalse(W3C.Internal.W3CUtilities.TryGetTraceId("|0123456789abcdef.1.2.3.54.", out _));
        }

        [TestMethod]
        public void ParseIncompatibleTraceIdParent_NotDotAt33()
        {
            Assert.IsFalse(W3C.Internal.W3CUtilities.TryGetTraceId("|0123456789abcdef0123456789abcdefx.1.2.3.54.", out _));
        }

        [TestMethod]
        public void ParseIncompatibleTraceIdParent_33Length()
        {
            Assert.IsFalse(W3C.Internal.W3CUtilities.TryGetTraceId("|0123456789abcdef0123456789abcdef", out _));
        }
    }
}
