namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;

#if NET40
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Reflection;

    /// <summary>
    /// Tests the rich payload event source tracking.
    /// </summary>
    [TestClass]
    public class RichPayloadEventSourceTest
    {
        /// <summary>
        /// Tests tracking request telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceRequestSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Requests,
                new RequestTelemetry("TestRequest", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", true),
                typeof(External.RequestData),
                (client, item) => { client.TrackRequest((RequestTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking trace telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceTraceSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Traces,
                new TraceTelemetry("TestTrace", SeverityLevel.Information),
                typeof(External.MessageData),
                (client, item) => { client.TrackTrace((TraceTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking event telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceEventSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Events,
                new EventTelemetry("TestEvent"),
                typeof(External.EventData),
                (client, item) => { client.TrackEvent((EventTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking exception telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceExceptionSentTest()
        {
            var exceptionTelemetry = new ExceptionTelemetry(new SystemException("Test"));
            exceptionTelemetry.Data.exceptions[0].parsedStack = new External.StackFrame[] { new External.StackFrame() };

            this.DoTracking(
                RichPayloadEventSource.Keywords.Exceptions,
                exceptionTelemetry,
                typeof(External.ExceptionData),
                (client, item) => { client.TrackException((ExceptionTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking metric telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceMetricSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Metrics,
                new MetricTelemetry("TestMetric", 1),
                typeof(External.MetricData),
                (client, item) => { client.TrackMetric((MetricTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking dependency telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceDependencySentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Dependencies,
                new DependencyTelemetry("TestDependency", "TestCommand", DateTimeOffset.Now, TimeSpan.Zero, true),
                typeof(External.RemoteDependencyData),
                (client, item) => { client.TrackDependency((DependencyTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking page view telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourcePageViewSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.PageViews,
                new PageViewTelemetry("TestPage"),
                typeof(External.PageViewData),
                (client, item) => { client.TrackPageView((PageViewTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking session state telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceSessionStateSentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.SessionState,
                new SessionStateTelemetry(new SessionState()),
                typeof(External.SessionStateData),
                (client, item) => { client.Track((SessionStateTelemetry)item); });
        }

        /// <summary>
        /// Tests tracking session state telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceSessionPerformanceCounterTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.PerformanceCounters,
                new PerformanceCounterTelemetry("TestCategory", "TestCounter", "TestInstance", 1.0),
                typeof(External.PerformanceCounterData),
                (client, item) => { client.Track((PerformanceCounterTelemetry)item); });
        }

        /// <summary>
        /// Helper method to setup shared context and call the desired tracking for testing.
        /// </summary>
        /// <param name="keywords">The event keywords to enable.</param>
        /// <param name="item">The telemetry item to track.</param>
        /// <param name="track">The tracking callback to execute.</param>
        private void DoTracking(EventKeywords keywords, ITelemetry item, Type dataType, Action<TelemetryClient, ITelemetry> track)
        {
            if (IsRunningOnEnvironmentSupportingRichPayloadEventSource())
            {
                var client = new TelemetryClient();
                client.InstrumentationKey = Guid.NewGuid().ToString();

                using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
                {
                    listener.EnableEvents(RichPayloadEventSource.Log.EventSourceInternal, EventLevel.Verbose, keywords);

                    item.Context.Properties.Add("property1", "value1");
                    item.Context.User.Id = "testUserId";
                    item.Context.Operation.Id = Guid.NewGuid().ToString();

                    track(client, item);

                    var actualEvent = listener.Messages.FirstOrDefault();

                    Assert.IsNotNull(actualEvent);
                    Assert.AreEqual(client.InstrumentationKey, actualEvent.Payload[0]);

                    int keysFound = 0;
                    object[] tags = actualEvent.Payload[1] as object[];
                    foreach (object tagObject in tags)
                    {
                        Dictionary<string, object> tag = (Dictionary<string, object>)tagObject;
                        Assert.IsNotNull(tag);
                        string key = (string)tag["Key"];
                        object value = tag["Value"];
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            if (key == "ai.user.id")
                            {
                                Assert.AreEqual("testUserId", value);
                                ++keysFound;
                            }
                            else if (key == "ai.operation.id")
                            {
                                Assert.AreEqual(item.Context.Operation.Id, value);
                                ++keysFound;
                            }
                        }
                    }

                    Assert.AreEqual(2, keysFound);
                    Assert.IsNotNull(actualEvent.Payload[2]);

                    var expectedProperties = dataType.GetProperties().AsEnumerable();
                    var actualPropertiesPayload = (IDictionary<string, object>)actualEvent.Payload[2];
                    VerifyEventPayload(expectedProperties, actualPropertiesPayload);
                }
            }
            else
            {
                // 4.5 doesn't have RichPayload events
                Assert.IsNull(RichPayloadEventSource.Log.EventSourceInternal);
            }
        }

        private static void VerifyEventPayload(IEnumerable<PropertyInfo> expectedProperties, IDictionary<string, object> actualEventPayload)
        {
            var actualProperties = actualEventPayload.Keys.Select(k => new { Key = k, Value = actualEventPayload[k] });

            Assert.IsTrue(expectedProperties.Count() == actualProperties.Count());
            var expectedPropertiesEnumerator = expectedProperties.GetEnumerator();
            var actualPropertiesEnumerator = actualProperties.GetEnumerator();
            while (expectedPropertiesEnumerator.MoveNext() && actualPropertiesEnumerator.MoveNext())
            {
                var expectedProperty = expectedPropertiesEnumerator.Current;
                var actualProperty = actualPropertiesEnumerator.Current;
                Assert.AreEqual(expectedPropertiesEnumerator.Current.Name, actualPropertiesEnumerator.Current.Key);

                if (!expectedProperty.PropertyType.IsValueType
                    && !expectedProperty.PropertyType.IsPrimitive
                    && expectedProperty.PropertyType != typeof(string))
                {
                    if (expectedProperty.PropertyType.IsClass)
                    {
                        VerifyEventPayload(expectedProperty.PropertyType.GetProperties().AsEnumerable(), (IDictionary<string, object>)actualProperty.Value);
                    }
                    else
                    {
                        var enumerableType = GetEnumerableType(expectedProperty.PropertyType);
                        Assert.IsNotNull(enumerableType);
                        Assert.IsTrue(actualProperty.Value.GetType().IsArray);

                        if (enumerableType.IsClass)
                        {
                            VerifyEventPayload(enumerableType.GetProperties().AsEnumerable(), (IDictionary<string, object>)((object[])actualProperty.Value)[0]);
                        }
                    }
                }

            }
        }

        private static Type GetEnumerableType(Type type)
        {
            foreach (Type intType in type.GetInterfaces())
            {
                if (intType.IsGenericType && intType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return intType.GetGenericArguments()[0];
                }
            }
            return null;
        }

        private static bool IsRunningOnEnvironmentSupportingRichPayloadEventSource()
        {
#if NET40
            // NET40 version uses EventSource in Microsoft.Diagnostics which supports RichPayloadEvent on .NET Framework 4.0/4.5/4.6+
            return true;
#else
            // Other versions depend on EventSource in .Net Framework 4.6+
            string productVersionString = System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(object).Assembly.Location).ProductVersion;

            Version ver;
            if (!Version.TryParse(productVersionString, out ver))
            {
                Assert.Fail("Unable to determine .net framework version");
            }

            var ver46 = new Version(4, 6, 0, 0);
            return ver >= ver46;
#endif
        }
    }
}