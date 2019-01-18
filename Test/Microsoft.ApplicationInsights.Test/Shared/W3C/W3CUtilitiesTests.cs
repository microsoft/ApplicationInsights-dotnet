namespace Microsoft.ApplicationInsights.W3C
{
    using System.Text.RegularExpressions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class W3CActivityUtilitiesTests
    {
        private static readonly Regex TraceIdRegex = new Regex("^[a-f0-9]{32}$", RegexOptions.Compiled);
        private static readonly Regex SpanIdRegex = new Regex("^[a-f0-9]{16}$", RegexOptions.Compiled);

        [TestMethod]
        public void GenerateTraceIdGeneratesValidId()
        {
            var traceId = W3CUtilities.GenerateTraceId();
            Assert.IsTrue(TraceIdRegex.IsMatch(traceId));
        }

        [TestMethod]
        public void GenerateSpanIdGeneratesValidId()
        {
            var spanId = W3CUtilities.GenerateSpanId();
            Assert.IsTrue(SpanIdRegex.IsMatch(spanId));
        }

    }
}
