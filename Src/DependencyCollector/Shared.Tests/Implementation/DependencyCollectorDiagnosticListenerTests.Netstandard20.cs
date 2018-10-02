namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.W3C;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Unit tests for DependencyCollectorDiagnosticListener.
    /// </summary>
    public partial class DependencyCollectorDiagnosticListenerTests
    {
        /// <summary>
        /// Tests that OnStartActivity injects headers.
        /// </summary>
        [TestMethod]
        public void OnActivityStartInjectsHeaders()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.AddBaggage("k", "v");
            activity.Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            // Request-Id and Correlation-Context are injected by HttpClient
            // check only legacy headers here
            Assert.IsFalse(requestMsg.Headers.Contains(RequestResponseHeaders.StandardRootIdHeader));
            Assert.IsFalse(requestMsg.Headers.Contains(RequestResponseHeaders.StandardParentIdHeader));
            Assert.AreEqual(this.testApplicationId1, GetRequestContextKeyValue(requestMsg, RequestResponseHeaders.RequestContextCorrelationSourceKey));
        }

        /// <summary>
        /// Tests that OnStartActivity injects headers.
        /// </summary>
        [TestMethod]
        public void OnActivityStartInjectsLegacyHeaders()
        {
            var listenerWithLegacyHeaders = new HttpCoreDiagnosticSourceListener(
                this.configuration,
                setComponentCorrelationHttpHeaders: true,
                correlationDomainExclusionList: new[] { "excluded.host.com" },
                injectLegacyHeaders: true,
                injectW3CHeaders: false);

            using (listenerWithLegacyHeaders)
            {
                var activity = new Activity("System.Net.Http.HttpRequestOut");
                activity.AddBaggage("k", "v");
                activity.Start();

                HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
                listenerWithLegacyHeaders.OnActivityStart(requestMsg);

                // Request-Id and Correlation-Context are injected by HttpClient
                // check only legacy headers here
                Assert.AreEqual(Activity.Current.RootId,
                    requestMsg.Headers.GetValues(RequestResponseHeaders.StandardRootIdHeader).Single());
                Assert.AreEqual(Activity.Current.Id,
                    requestMsg.Headers.GetValues(RequestResponseHeaders.StandardParentIdHeader).Single());
                Assert.AreEqual(this.testApplicationId1,
                    GetRequestContextKeyValue(requestMsg, RequestResponseHeaders.RequestContextCorrelationSourceKey));
            }
        }

#pragma warning disable 612, 618
        /// <summary>
        /// Tests that OnStartActivity injects W3C headers.
        /// </summary>
        [TestMethod]
        public void OnActivityStartInjectsW3CHeaders()
        {
            var listenerWithW3CHeaders = new HttpCoreDiagnosticSourceListener(
                this.configuration,
                setComponentCorrelationHttpHeaders: true,
                correlationDomainExclusionList: new[] { "excluded.host.com" },
                injectLegacyHeaders: false,
                injectW3CHeaders: true);

            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());

            using (listenerWithW3CHeaders)
            {
                var activity = new Activity("System.Net.Http.HttpRequestOut").SetParentId("|guid.").Start();

                HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
                listenerWithW3CHeaders.OnActivityStart(requestMsg);

                // Request-Id and Correlation-Context are injected by HttpClient
                // check only W3C headers here
                Assert.AreEqual(this.testApplicationId1, GetRequestContextKeyValue(requestMsg, RequestResponseHeaders.RequestContextCorrelationSourceKey));
                Assert.AreEqual($"00-{activity.GetTraceId()}-{activity.GetSpanId()}-02", requestMsg.Headers.GetValues(W3CConstants.TraceParentHeader).Single());
                Assert.AreEqual($"{W3CConstants.AzureTracestateNamespace}={this.testApplicationId1}", requestMsg.Headers.GetValues(W3CConstants.TraceStateHeader).Single());
            }
        }

        /// <summary>
        /// Tests that OnStartActivity injects W3C headers.
        /// </summary>
        [TestMethod]
        public void OnActivityStartInjectsW3CHeadersAndTracksLegacyId()
        {
            var listenerWithW3CHeaders = new HttpCoreDiagnosticSourceListener(
                this.configuration,
                setComponentCorrelationHttpHeaders: true,
                correlationDomainExclusionList: new string[0],
                injectLegacyHeaders: false,
                injectW3CHeaders: true);

            this.configuration.TelemetryInitializers.Add(new W3COperationCorrelationTelemetryInitializer());
            using (listenerWithW3CHeaders)
            {
                var activity = new Activity("System.Net.Http.HttpRequestOut").SetParentId("foo").Start();

                HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
                listenerWithW3CHeaders.OnActivityStart(requestMsg);

                // simulate Request-Id injection by .NET
                requestMsg.Headers.Add(RequestResponseHeaders.RequestIdHeader, activity.Id);

                listenerWithW3CHeaders.OnActivityStop(new HttpResponseMessage(HttpStatusCode.OK), requestMsg, TaskStatus.RanToCompletion);

                var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;
                Assert.IsNotNull(telemetry);
                Assert.IsTrue(telemetry.Properties.ContainsKey(W3CConstants.LegacyRequestIdProperty));
                Assert.AreEqual(activity.Id, telemetry.Properties[W3CConstants.LegacyRequestIdProperty]);

                Assert.IsTrue(telemetry.Properties.ContainsKey(W3CConstants.LegacyRootIdProperty));
                Assert.AreEqual(activity.RootId, telemetry.Properties[W3CConstants.LegacyRootIdProperty]);
            }
        }

#pragma warning restore 612, 618

        /// <summary>
        /// Tests that OnStopActivity tracks telemetry.
        /// </summary>
        [TestMethod]
        public async Task OnActivityStopTracksTelemetry()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut")
                .AddBaggage("k", "v")
                .Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            var approxStartTime = DateTime.UtcNow;
            activity = Activity.Current;

            await Task.Delay(10);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual("POST /", telemetry.Name);
            Assert.AreEqual(RequestUrl, telemetry.Target);
            Assert.AreEqual(RemoteDependencyConstants.HTTP, telemetry.Type);
            Assert.AreEqual(RequestUrlWithScheme, telemetry.Data);
            Assert.AreEqual("200", telemetry.ResultCode);
            Assert.AreEqual(true, telemetry.Success);

            Assert.IsTrue(Math.Abs((telemetry.Timestamp - approxStartTime).TotalMilliseconds) < 100);
            Assert.IsTrue(telemetry.Duration.TotalMilliseconds > 10);

            Assert.AreEqual(activity.RootId, telemetry.Context.Operation.Id);
            Assert.AreEqual(activity.ParentId, telemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, telemetry.Id);
            Assert.AreEqual("v", telemetry.Properties["k"]);

            string expectedVersion =
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rdddsc:");
            Assert.AreEqual(expectedVersion, telemetry.Context.GetInternalContext().SdkVersion);

            // Check the operation details
            this.operationDetailsInitializer.ValidateOperationDetailsCore(telemetry);
        }

        /// <summary>
        /// Tests that activity without parent gets a new W3C compatible root id.
        /// </summary>
        [TestMethod]
        public void OnActivityWithoutParentGeneratesW3CTraceId()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.AddBaggage("k", "v");
            var startTime = DateTime.UtcNow.AddSeconds(-1);
            activity.SetStartTime(startTime);
            activity.Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            // W3C compatible-Id ( should go away when W3C is implemented in .NET https://github.com/dotnet/corefx/issues/30331 TODO)
            Assert.IsNotNull(telemetry);
            Assert.AreEqual(32, telemetry.Context.Operation.Id.Length);
            Assert.IsTrue(Regex.Match(telemetry.Context.Operation.Id, @"[a-z][0-9]").Success);
            // end of workaround test
        }

        /// <summary>
        /// Tests that activity without parent id does not get a new W3C compatible root id.
        /// </summary>
        [TestMethod]
        public void OnActivityWithParentId()
        {
            new Activity("System.Net.Http.HttpRequestOut")
                .SetParentId("parent")
                .Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual("parent", telemetry.Context.Operation.Id);
            Assert.AreEqual("parent", telemetry.Context.Operation.ParentId);
        }

        /// <summary>
        /// Tests that activity without parent does not get a new W3C compatible root id.
        /// </summary>
        [TestMethod]
        public void OnActivityWithParent()
        {
            var parent = new Activity("dummy").Start();
            new Activity("System.Net.Http.HttpRequestOut").Start();
 
            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual(parent.RootId, telemetry.Context.Operation.Id);
            Assert.AreEqual(parent.Id, telemetry.Context.Operation.ParentId);
        }

        /// <summary>
        /// Tests that OnStopActivity tracks telemetry.
        /// </summary>
        [TestMethod]
        public void OnActivityStopWithParentTracksTelemetry()
        {
            var parent = new Activity("parent")
                .AddBaggage("k", "v")
                .Start();

            var activity = new Activity("System.Net.Http.HttpRequestOut").Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual(parent.RootId, telemetry.Context.Operation.Id);
            Assert.AreEqual(parent.Id, telemetry.Context.Operation.ParentId);
            Assert.AreEqual(activity.Id, telemetry.Id);
            Assert.AreEqual("v", telemetry.Properties["k"]);

            string expectedVersion =
                SdkVersionHelper.GetExpectedSdkVersion(typeof(DependencyTrackingTelemetryModule), prefix: "rdddsc:");
            Assert.AreEqual(expectedVersion, telemetry.Context.GetInternalContext().SdkVersion);

            // Check the operation details
            this.operationDetailsInitializer.ValidateOperationDetailsCore(telemetry);
        }

        /// <summary>
        /// Tests that OnStopActivity tracks cancelled request.
        /// </summary>
        [TestMethod]
        public void OnActivityStopTracksTelemetryForCanceledRequest()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            this.listener.OnActivityStop(null, requestMsg, TaskStatus.Canceled);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual("Canceled", telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);

            // Check the operation details
            this.operationDetailsInitializer.ValidateOperationDetailsCore(telemetry, responseExpected: false);
        }

        /// <summary>
        /// Tests that OnStopActivity tracks faulted request.
        /// </summary>
        [TestMethod]
        public void OnActivityStopTracksTelemetryForFaultedRequest()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            this.listener.OnActivityStop(null, requestMsg, TaskStatus.Faulted);

            var telemetry = this.sentTelemetry.Single() as DependencyTelemetry;

            Assert.IsNotNull(telemetry);
            Assert.AreEqual("Faulted", telemetry.ResultCode);
            Assert.AreEqual(false, telemetry.Success);

            // Check the operation details
            this.operationDetailsInitializer.ValidateOperationDetailsCore(telemetry, responseExpected: false);
        }

        /// <summary>
        /// Tests that exception during request processing is tracked with correct context.
        /// </summary>
        [TestMethod]
        public void OnExceptionTracksException()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            var exception = new HttpRequestException("message", new Exception("The server name or address could not be resolved"));
            this.listener.OnException(exception, requestMsg);
            this.listener.OnActivityStop(null, requestMsg, TaskStatus.Faulted);

            var dependencyTelemetry = this.sentTelemetry.Single(t => t is DependencyTelemetry) as DependencyTelemetry;
            var exceptionTelemetry = this.sentTelemetry.Single(t => t is ExceptionTelemetry) as ExceptionTelemetry;

            Assert.AreEqual(2, this.sentTelemetry.Count);
            Assert.IsNotNull(exceptionTelemetry);
            Assert.IsNotNull(dependencyTelemetry);
            Assert.AreEqual(exception, exceptionTelemetry.Exception);
            Assert.AreEqual(exceptionTelemetry.Context.Operation.Id, dependencyTelemetry.Context.Operation.Id);
            Assert.AreEqual(exceptionTelemetry.Context.Operation.ParentId, dependencyTelemetry.Id);
            Assert.AreEqual("The server name or address could not be resolved", dependencyTelemetry.Properties["Error"]);

            // Check the operation details
            this.operationDetailsInitializer.ValidateOperationDetailsCore(dependencyTelemetry, responseExpected: false);
        }

        /// <summary>
        /// Tests HTTP dependencies and exceptions are NOT tracked for ApplicationInsights URL.
        /// </summary>
        [TestMethod]
        public void ApplicationInsightsUrlAreNotTracked()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut");
            activity.Start();

            var appInsightsUrl = TelemetryConfiguration.Active.TelemetryChannel.EndpointAddress;
            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Get, appInsightsUrl);
            this.listener.OnActivityStart(requestMsg);
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.RequestContextCorrelationSourceKey));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.RequestIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.StandardParentIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.StandardRootIdHeader));

            var exception = new HttpRequestException("message", new Exception("The server name or address could not be resolved"));
            this.listener.OnException(exception, requestMsg);
            this.listener.OnActivityStop(null, requestMsg, TaskStatus.Faulted);

            Assert.IsFalse(this.sentTelemetry.Any());
        }

        /// <summary>
        /// Call OnStartActivity() with uri that is in the excluded domain list.
        /// </summary>
        [TestMethod]
        public void OnStartActivityWithUriInExcludedDomainList()
        {
            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, "http://excluded.host.com/path/to/file.html");
            this.listener.OnActivityStart(requestMsg);
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.RequestContextCorrelationSourceKey));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.RequestIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.StandardParentIdHeader));
            Assert.IsNull(HttpHeadersUtilities.GetRequestContextKeyValue(requestMsg.Headers, RequestResponseHeaders.StandardRootIdHeader));
        }

        /// <summary>
        /// Tests that if OnStopActivity is called with null Activity, dependency is not tracked
        /// </summary>
        [TestMethod]
        public async Task OnActivityStopWithNullActivityDoesNotTrackDependency()
        {
            var activity = new Activity("System.Net.Http.HttpRequestOut")
                .AddBaggage("k", "v")
                .Start();

            HttpRequestMessage requestMsg = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            this.listener.OnActivityStart(requestMsg);

            await Task.Delay(10);

            activity = Activity.Current;
            activity.Stop();

            Assert.IsNull(Activity.Current);

            HttpResponseMessage responseMsg = new HttpResponseMessage(HttpStatusCode.OK);
            this.listener.OnActivityStop(responseMsg, requestMsg, TaskStatus.RanToCompletion);

            Assert.IsFalse(this.sentTelemetry.Any());
        }

        [TestMethod]
        public void MultiHost_OnlyOneListnerTracksTelemetry()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var startEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start",
                    new { Request = request });
            var stopEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop",
                    new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });

            using (var secondListener = this.CreateHttpListener())
            {
                var activity = new Activity("System.Net.Http.HttpRequestOut").Start();

                this.listener.OnNext(startEvent);
                secondListener.OnNext(startEvent);

                this.listener.OnNext(stopEvent);
                secondListener.OnNext(stopEvent);

                Assert.AreEqual(1, this.sentTelemetry.Count(t => t is DependencyTelemetry));
            }
        }

        [TestMethod]
        public void MultiHost_TwoActiveAndOneIsDisposedStillTracksTelemetry()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var startEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start",
                    new { Request = request });
            var stopEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop",
                    new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });

            using (var secondListener = this.CreateHttpListener())
            {
                var activity = new Activity("System.Net.Http.HttpRequestOut").Start();
                this.listener.OnNext(startEvent);
                secondListener.OnNext(startEvent);

                this.listener.OnNext(stopEvent);
                secondListener.OnNext(stopEvent);

                Assert.AreEqual(1, this.sentTelemetry.Count(t => t is DependencyTelemetry));

                this.listener.Dispose();

                activity = new Activity("System.Net.Http.HttpRequestOut").Start();
                secondListener.OnNext(startEvent);
                secondListener.OnNext(stopEvent);

                Assert.AreEqual(2, this.sentTelemetry.Count(t => t is DependencyTelemetry));
            }
        }

        [TestMethod]
        public void MultiHost_OneListnerThenAnotherTracksTelemetry()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, RequestUrlWithScheme);
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var startEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Start",
                    new { Request = request });
            var stopEvent =
                new KeyValuePair<string, object>("System.Net.Http.HttpRequestOut.Stop",
                    new { Request = request, Response = response, RequestTaskStatus = TaskStatus.RanToCompletion });

            var activity = new Activity("System.Net.Http.HttpRequestOut").Start();
            this.listener.OnNext(startEvent);
            this.listener.OnNext(stopEvent);

            Assert.AreEqual(1, this.sentTelemetry.Count(t => t is DependencyTelemetry));
            
            this.listener.Dispose();

            using (var secondListener = this.CreateHttpListener())
            {
                activity = new Activity("System.Net.Http.HttpRequestOut").Start();
                secondListener.OnNext(startEvent);
                secondListener.OnNext(stopEvent);

                Assert.AreEqual(2, this.sentTelemetry.Count(t => t is DependencyTelemetry));
            }
        }

        private HttpCoreDiagnosticSourceListener CreateHttpListener()
        {
            return new HttpCoreDiagnosticSourceListener(
                this.configuration,
                setComponentCorrelationHttpHeaders: true,
                correlationDomainExclusionList: new string[0],
                injectLegacyHeaders: false,
                injectW3CHeaders: false);
        }
    }
}