namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Collections.Generic;
#if NET45
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;

    using Microsoft.ApplicationInsights.Web.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebEventsSubscriberTest
    {
        private const long AllKeywords = -1;

        [TestMethod]
        public void HanderIsCalledForOnBegin()
        {
            bool called = false;
            var handlers = new Dictionary<int, Action<EventWrittenEventArgs>>();
            handlers.Add(1, args => called = true);
            
            using (new WebEventsSubscriber(handlers))
            {
                WebEventsPublisher.Log.OnBegin();
            }

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void HanderIsCalledForOnEnd()
        {
            bool called = false;
            var handlers = new Dictionary<int, Action<EventWrittenEventArgs>>();
            handlers.Add(2, args => called = true);

            using (new WebEventsSubscriber(handlers))
            {
                WebEventsPublisher.Log.OnEnd();
            }

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void HanderIsCalledForOnError()
        {
            bool called = false;
            var handlers = new Dictionary<int, Action<EventWrittenEventArgs>>();
            handlers.Add(3, args => called = true);

            using (new WebEventsSubscriber(handlers))
            {
                WebEventsPublisher.Log.OnError();
            }

            Assert.IsTrue(called);
        }

        [TestMethod]
        public void HanderIsNotCalledIfHanderNotProvided()
        {
            bool called = false;
            var handlers = new Dictionary<int, Action<EventWrittenEventArgs>>();
            handlers.Add(42, args => called = true);

            using (new WebEventsSubscriber(handlers))
            {
                WebEventsPublisher.Log.OnBegin();
                WebEventsPublisher.Log.OnEnd();
                WebEventsPublisher.Log.OnError();
            }

            Assert.IsFalse(called);
        }

        [TestMethod]
        public void ErrorLoggedIfHandlerThrowsException()
        {
            var handlers = new Dictionary<int, Action<EventWrittenEventArgs>>();
            handlers.Add(1, args => { throw new ApplicationException(); });
            
            using (new WebEventsSubscriber(handlers))
            {
                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(WebEventSource.Log, EventLevel.Error, (EventKeywords)AllKeywords);

                    WebEventsPublisher.Log.OnBegin();

                    var firstEvent = listener.Messages.FirstOrDefault();
                    Assert.IsNotNull(firstEvent);
                    Assert.AreEqual(4, firstEvent.EventId);
                }
            }
        }
    }
}
