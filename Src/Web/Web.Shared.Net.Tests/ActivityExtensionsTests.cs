namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ActivityExtensionsTests
    {
        [TestCleanup]
        public void Cleanup()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }

        [TestMethod]
        public void UpdateParentCopiesParentActivity()
        {
            Activity originalActivity = new Activity("dummy").Start();

            var newActivity = originalActivity.UpdateParent("newparent");

            Assert.AreEqual(originalActivity.OperationName, newActivity.OperationName);
            Assert.AreEqual(originalActivity.StartTimeUtc, newActivity.StartTimeUtc);
            Assert.AreEqual(TimeSpan.Zero, newActivity.Duration);
            Assert.IsFalse(newActivity.Tags.Any());
            Assert.IsFalse(newActivity.Baggage.Any());

            Assert.IsNull(newActivity.Parent);
            Assert.AreEqual("newparent", newActivity.ParentId);
            Assert.AreEqual("newparent", newActivity.RootId);
            Assert.IsNotNull(newActivity.Id);
        }

        [TestMethod]
        public void UpdateParentCopiesParentActivityTags()
        {
            Activity originalActivity = new Activity("dummy")
                .AddTag("tk1", "tv1")
                .AddTag("tk2", "tv2")
                .Start();

            var newActivity = originalActivity.UpdateParent("newparent");
            Assert.AreEqual(1, newActivity.Tags.Count(t => t.Key == "tk1" && t.Value == "tv1"));
            Assert.AreEqual(1, newActivity.Tags.Count(t => t.Key == "tk2" && t.Value == "tv2"));
            Assert.AreEqual(2, newActivity.Tags.Count());
        }

        [TestMethod]
        public void UpdateParentCopiesParentActivityBaggage()
        {
            Activity originalActivity = new Activity("dummy")
                .AddBaggage("bk1", "bv1")
                .AddBaggage("bk2", "bv2")
                .Start();

            var newActivity = originalActivity.UpdateParent("newparent");
            Assert.AreEqual(1, newActivity.Baggage.Count(t => t.Key == "bk1" && t.Value == "bv1"));
            Assert.AreEqual(1, newActivity.Baggage.Count(t => t.Key == "bk2" && t.Value == "bv2"));
            Assert.AreEqual(2, newActivity.Baggage.Count());
        }

        [TestMethod]
        public void UpdateParentCopiesNotStartedActivity()
        {
            Activity originalActivity = new Activity("dummy");

            var newActivity = originalActivity.UpdateParent("newparent");

            Assert.AreNotEqual(originalActivity.StartTimeUtc, newActivity.StartTimeUtc);
            Assert.AreEqual(TimeSpan.Zero, newActivity.Duration);

            Assert.IsNull(newActivity.Parent);
            Assert.AreEqual("newparent", newActivity.ParentId);
            Assert.AreEqual("newparent", newActivity.RootId);
            Assert.IsNotNull(newActivity.Id);
        }
    }
}
