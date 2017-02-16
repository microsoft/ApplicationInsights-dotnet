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
#pragma warning disable CS0618
                new MetricTelemetry("TestMetric", 1),
                typeof(External.MetricData),
                (client, item) => { client.TrackMetric((MetricTelemetry)item); });
#pragma warning restore CS0618
        }

        /// <summary>
        /// Tests tracking dependency telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceDependencySentTest()
        {
            this.DoTracking(
                RichPayloadEventSource.Keywords.Dependencies,
                new DependencyTelemetry("Custom", "Target", "TestDependency", "TestCommand", DateTimeOffset.Now, TimeSpan.Zero, "200", true),
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
#pragma warning disable 618
            this.DoTracking(
                RichPayloadEventSource.Keywords.Events,
                new SessionStateTelemetry(new SessionState()),
                typeof(External.EventData),
                (client, item) => { client.Track((SessionStateTelemetry)item); });
#pragma warning restore 618
        }

        /// <summary>
        /// Tests tracking session state telemetry.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceSessionPerformanceCounterTest()
        {
#pragma warning disable 618
            this.DoTracking(
                RichPayloadEventSource.Keywords.Metrics,
                new PerformanceCounterTelemetry("TestCategory", "TestCounter", "TestInstance", 1.0),
                typeof(External.MetricData),
                (client, item) => { client.Track((PerformanceCounterTelemetry)item); });
#pragma warning restore 618
        }

        /// <summary>
        /// Tests start/stop events for Operations.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceOperationStartStopTest()
        {
            if (IsRunningOnEnvironmentSupportingRichPayloadEventSource())
            {
                var client = CreateTelemetryClient();

                using (var listener = new TestFramework.TestEventListener())
                {
                    listener.EnableEvents(RichPayloadEventSource.Log.EventSourceInternal, EventLevel.Informational, RichPayloadEventSource.Keywords.Operations);

                    // Simulate a Start/Stop request operation
                    var requestTelemetry = new RequestTelemetry { Name = "Request" };
                    using (client.StartOperation(requestTelemetry))
                    {
                    }

                    // Expect exactly two events (start and stop)
                    var actualEvents = listener.Messages.Where(m => m.Keywords.HasFlag(RichPayloadEventSource.Keywords.Operations)).Take(2).ToArray();

                    VerifyOperationEvent(requestTelemetry, RequestTelemetry.TelemetryName, EventOpcode.Start, actualEvents[0]);
                    VerifyOperationEvent(requestTelemetry, RequestTelemetry.TelemetryName, EventOpcode.Stop, actualEvents[1]);
                }
            }
            else
            {
                // 4.5 doesn't have RichPayload events
                Assert.IsNull(RichPayloadEventSource.Log.EventSourceInternal);
            }
        }

        /// <summary>
        /// Tests start/stop events for nested operations.
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceNestedOperationStartStopTest()
        {
            if (IsRunningOnEnvironmentSupportingRichPayloadEventSource())
            {
                var client = CreateTelemetryClient();

                using (var listener = new TestFramework.TestEventListener())
                {
                    listener.EnableEvents(RichPayloadEventSource.Log.EventSourceInternal, EventLevel.Informational, RichPayloadEventSource.Keywords.Operations);

                    // Simulate a Start/Stop request operation
                    var requestTelemetry = new RequestTelemetry { Name = "Request" };
                    var nestedOperation = new DependencyTelemetry { Name = "Dependency" };
                    using (client.StartOperation(requestTelemetry))
                    {
                        using (client.StartOperation(nestedOperation))
                        {
                        }
                    }

                    // Expect exactly four events (start, start, stop, stop)
                    var actualEvents = listener.Messages.Where(m=>m.Keywords.HasFlag(RichPayloadEventSource.Keywords.Operations)).Take(4).ToArray();

                    VerifyOperationEvent(requestTelemetry, RequestTelemetry.TelemetryName, EventOpcode.Start, actualEvents[0]);
                    VerifyOperationEvent(nestedOperation, OperationTelemetry.TelemetryName, EventOpcode.Start, actualEvents[1]);
                    VerifyOperationEvent(nestedOperation, OperationTelemetry.TelemetryName, EventOpcode.Stop, actualEvents[2]);
                    VerifyOperationEvent(requestTelemetry, RequestTelemetry.TelemetryName, EventOpcode.Stop, actualEvents[3]);
                }
            }
            else
            {
                // 4.5 doesn't have RichPayload events
                Assert.IsNull(RichPayloadEventSource.Log.EventSourceInternal);
            }
        }

        /// <summary>
        /// Tests sanitizing telemetry event
        /// </summary>
        [TestMethod]
        public void RichPayloadEventSourceSanitizeTest()
        {
            if (IsRunningOnEnvironmentSupportingRichPayloadEventSource())
            {
                var request = new RequestTelemetry()
                {
                    Name = new String('n', 5000),
                    Url = new Uri("https://www.bing.com/" + new String('u', 5000)),
                    ResponseCode = "200"
                };

                var client = CreateTelemetryClient();

                using (var listener = new Microsoft.ApplicationInsights.TestFramework.TestEventListener())
                {
                    listener.EnableEvents(RichPayloadEventSource.Log.EventSourceInternal, EventLevel.Verbose, RichPayloadEventSource.Keywords.Requests);

                    client.Track(request);

                    IDictionary<string, object> richPayload = (IDictionary<string, object>)listener.Messages.FirstOrDefault().Payload[2];

                    Assert.AreEqual(Property.MaxNameLength, richPayload["name"].ToString().Length);
                    Assert.AreEqual(Property.MaxUrlLength, richPayload["url"].ToString().Length);
                    Assert.AreEqual(true, richPayload["success"]);
                };
            }
        }


        private TelemetryClient CreateTelemetryClient()
        {
            // The default InMemoryChannel creates a worker thread which, if left running, causes
            // System.AppDomainUnloadedException from the test runner.
            var channel = new TestFramework.StubTelemetryChannel();
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString(), channel);
            var client = new TelemetryClient(configuration) { InstrumentationKey = configuration.InstrumentationKey };
            return client;
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
                var client = CreateTelemetryClient();

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

        private static void VerifyOperationEvent(OperationTelemetry expectedOperation, string expectedName, EventOpcode expectedOpCode, EventWrittenEventArgs actualEvent)
        {
            Assert.AreEqual(expectedOpCode, actualEvent.Opcode);
#if !NET45
            Assert.AreEqual(expectedName, actualEvent.EventName);
#endif
            VerifyOperationPayload(expectedOperation, actualEvent.Payload);
        }

        private static void VerifyOperationPayload(OperationTelemetry expected, IReadOnlyList<object> actualPayload)
        {
            Assert.IsNotNull(actualPayload);
            Assert.AreEqual(4, actualPayload.Count);
            Assert.AreEqual(expected.Context.InstrumentationKey, actualPayload[0]);
            Assert.AreEqual(expected.Id, actualPayload[1]);
            Assert.AreEqual(expected.Name, actualPayload[2]);
            Assert.AreEqual(expected.Context.Operation.Id, actualPayload[3]);
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