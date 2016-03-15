namespace Microsoft.ApplicationInsights.Web
{
    using System;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Globalization;
    using System.Linq;
    using System.Web;

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
        private PrivateObject module2;

        [TestInitialize]
        public void Initialize()
        {
            this.module = HttpModuleHelper.CreateTestModule();
            this.module2 = HttpModuleHelper.CreateTestModule();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ((IHttpModule)this.module.Target).Dispose();
            ((IHttpModule)this.module2.Target).Dispose();
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

        [TestMethod]
        public void OnEndGeneratesEventsOnlyFromOneModuleInstanceIfItSharesSameHttpContext()
        {
            var httpApplication = HttpModuleHelper.GetFakeHttpApplication();

            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { httpApplication, null }, CultureInfo.InvariantCulture);
                this.module2.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { httpApplication, null }, CultureInfo.InvariantCulture);

                var count = listener.Messages.Count();
                Assert.AreEqual(2, count); // OnEnd and OnError
            }
        }

        [TestMethod]
        public void OnEndGeneratesEventsFromAllModuleInstancesIfTheyDoNotSharesSameHttpContext()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(WebEventsPublisher.Log, EventLevel.LogAlways, (EventKeywords)AllKeywords);

                this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { HttpModuleHelper.GetFakeHttpApplication(), null }, CultureInfo.InvariantCulture);
                this.module2.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { HttpModuleHelper.GetFakeHttpApplication(), null }, CultureInfo.InvariantCulture);

                var count = listener.Messages.Count();
                Assert.AreEqual(4, count); // OnEnd and OnError * 2
            }
        }

        [TestMethod]
        public void OnEndAddsFladInHttpContext()
        {
            var httpApplication = HttpModuleHelper.GetFakeHttpApplication();

            this.module.Invoke("OnEndRequest", new[] { typeof(object), typeof(EventArgs) }, new object[] { httpApplication, null }, CultureInfo.InvariantCulture);

            Assert.IsNotNull(httpApplication.Context.Items[RequestTrackingConstants.EndRequestCallFlag]);
        }
    }
}