#if !NET40
namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;

    [TestClass]
    public class ActivityExtensionsTests
    {
        [TestMethod]
        public void CanLoadDiagnosticSourceAssembly()
        {
            Assert.True(ActivityExtensions.TryRun(() => Assert.Null(Activity.Current)));
        }

        [TestMethod]
        public void GetOperationNameReturnsNullIfThereIsNoOperationName()
        {
            var activity = new Activity("test me");
            Assert.Null(activity.GetOperationName());
        }

        [TestMethod]
        public void SetOperationNameIsConsistentWithGetOperationName()
        {
            var activity = new Activity("test");
            activity.SetOperationName("test me");
            Assert.Equal("test me", activity.GetOperationName());
        }

        [TestMethod]
        public void GetOperationNameReturnsLastAddedOperationName()
        {
            var activity = new Activity("test");
            activity.AddTag("OperationName", "test me 1");
            activity.AddTag("OperationName", "test me 2");

            Assert.Equal("test me 2", activity.GetOperationName());
        }
    }
}
#endif