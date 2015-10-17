namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif 

    [TestClass]
    public class OperationWatchTests
    {
        [TestMethod]
        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:ParameterMustNotSpanMultipleLines",
            Justification = "Assert text parameters span multiple lines - no point separating them")]
        public void OperationWatchMatchesDateTimeOffset()
        {
            long elapsedTicks = OperationWatch.ElapsedTicks;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            DateTimeOffset nowAccordingToOperationWatch = OperationWatch.Timestamp(elapsedTicks);

            // since getting elapsed ticks and 'utcnow' on DateTimeOffset
            // are two different operations, the timestamps are going to
            // be different but close
            const int DeltaMilliseconds = 100;

            Assert.True(
                nowAccordingToOperationWatch >= now.AddMilliseconds(-DeltaMilliseconds)
                && nowAccordingToOperationWatch <= now.AddMilliseconds(DeltaMilliseconds),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Timestamp per operation watch {0} differs from timestamp of test {1} by {2} which is outside of the tolerance of {3}ms",
                    nowAccordingToOperationWatch.ToString("O", CultureInfo.InvariantCulture),
                    now.ToString("O", CultureInfo.InvariantCulture),
                    (now - nowAccordingToOperationWatch).ToString("G", CultureInfo.InvariantCulture),
                    DeltaMilliseconds));
        }
    }
}
