namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
#if NET40 || NET45 || NET35 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    using Assert = Xunit.Assert;
    using DpSeverityLevel = Microsoft.ApplicationInsights.DataContracts.SeverityLevel;
    
    [TestClass]
    public class SeverityLevelExtensionsTest
    {
        [TestMethod]
        public void TranslateSeverityLevelConvertsAllValueFromSdkToDp()
        {
            Assert.Equal(DpSeverityLevel.Verbose, SeverityLevelExtensions.TranslateSeverityLevel(SeverityLevel.Verbose));
            Assert.Equal(DpSeverityLevel.Warning, SeverityLevelExtensions.TranslateSeverityLevel(SeverityLevel.Warning));
            Assert.Equal(DpSeverityLevel.Information, SeverityLevelExtensions.TranslateSeverityLevel(SeverityLevel.Information));
            Assert.Equal(DpSeverityLevel.Error, SeverityLevelExtensions.TranslateSeverityLevel(SeverityLevel.Error));
            Assert.Equal(DpSeverityLevel.Critical, SeverityLevelExtensions.TranslateSeverityLevel(SeverityLevel.Critical));
        }

        [TestMethod]
        public void TranslateSeverityLevelConvertsAllValueFromDpToSdk()
        {
            Assert.Equal(SeverityLevel.Verbose, SeverityLevelExtensions.TranslateSeverityLevel(DpSeverityLevel.Verbose));
            Assert.Equal(SeverityLevel.Warning, SeverityLevelExtensions.TranslateSeverityLevel(DpSeverityLevel.Warning));
            Assert.Equal(SeverityLevel.Information, SeverityLevelExtensions.TranslateSeverityLevel(DpSeverityLevel.Information));
            Assert.Equal(SeverityLevel.Error, SeverityLevelExtensions.TranslateSeverityLevel(DpSeverityLevel.Error));
            Assert.Equal(SeverityLevel.Critical, SeverityLevelExtensions.TranslateSeverityLevel(DpSeverityLevel.Critical));
        }
    }
}
