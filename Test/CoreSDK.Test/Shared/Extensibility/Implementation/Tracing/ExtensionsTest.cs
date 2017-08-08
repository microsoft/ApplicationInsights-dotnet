namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
#if !NETCOREAPP1_1
    using System;
    using System.Globalization;
    using System.Threading;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    public class ExtensionsTest
    {
        [TestClass]
        public class ToInvariantString
        {
            private CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;

            [TestCleanup]
            public void Cleanup()
            {
                Thread.CurrentThread.CurrentUICulture = this.originalUICulture;
            }

            [TestMethod]
            public void ExtractsStackTraceWithInvariantCultureToHelpOurTelemetryToolsMatchSimilarErrorsReportedByOSsWithDifferentLanguages()
            {
                CultureInfo stackTraceCulture = null;
                var exception = new StubException();
                exception.OnToString = () =>
                {
                    stackTraceCulture = Thread.CurrentThread.CurrentUICulture;
                    return string.Empty;
                };

                Extensions.ToInvariantString(exception);

                Assert.Same(CultureInfo.InvariantCulture, stackTraceCulture);
            }

            [TestMethod]
            public void RestoresOriginalUICultureToPreserveGlobalStateOfApplication()
            {
                Extensions.ToInvariantString(new Exception());
                Assert.Same(this.originalUICulture, Thread.CurrentThread.CurrentUICulture);
            }
        }
    }
#endif
}
