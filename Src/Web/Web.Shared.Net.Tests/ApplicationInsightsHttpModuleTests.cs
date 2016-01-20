namespace Microsoft.ApplicationInsights.Web
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ApplicationInsightsHttpModuleTests
    {
        private const long AllKeywords = -1;

        private PrivateObject module;

        [TestInitialize]
        public void Initialize()
        {
            this.module = HttpModuleHelper.CreateTestModule();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ((IHttpModule)this.module.Target).Dispose();
        }

        [TestMethod]
        public void OnBeginGeneratesWebEventsOnBeginEvent()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                this.module.Invoke("OnBeginRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { HttpModuleHelper.GetFakeHttpApplication(), null }, CultureInfo.InvariantCulture);

                var firstEvent = listener.Messages.FirstOrDefault();
                Assert.IsNotNull(firstEvent);
                Assert.AreEqual(1, firstEvent.EventId);
            }
        }

        [TestMethod]
        public void OnEndGeneratesWebEventsOnEndEvent()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { HttpModuleHelper.GetFakeHttpApplication(), null }, CultureInfo.InvariantCulture);

                var messages = listener.Messages.OrderBy(_ => _.EventId).ToList();
                Assert.AreEqual(2, messages[0].EventId);
            }
        }

        [TestMethod]
        public void OnEndGeneratesWebEventsOnErrorEvent()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { HttpModuleHelper.GetFakeHttpApplication(), null }, CultureInfo.InvariantCulture);

                var messages = listener.Messages.OrderBy(_ => _.EventId).ToList();
                Assert.AreEqual(3, messages[1].EventId);
            }
        }
    }
}