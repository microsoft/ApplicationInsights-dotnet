using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using WireMock.Logging;
using WireMock.Server;
using Formatting = Newtonsoft.Json.Formatting;
using Request = WireMock.RequestBuilders.Request;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class TelemetryClientHttpMockTest : IDisposable
{
    private const string NodeName = "node-01";

    private readonly WireMockServer _mockServer;
    private readonly string _testConnectionString;

    private const bool _testDebug = false;

    [TestMethod]
    public async Task TrackEvent()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackEvent("TestEvent");
        await VerifyTrackMethod(ClientConsumer, "expected-event.json");
    }

    [TestMethod]
    public async Task TrackEventWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackEvent("TestEventWithProperties", properties);

        await VerifyTrackMethod(ClientConsumer, "expected-event-with-properties.json");
    }

    [TestMethod]
    public async Task TrackEventWithPropertiesAndMetrics()
    {
        var properties = new Dictionary<string, string>
        {
            { "Category", "UserActions" },
            { "Source", "MobileApp" }
        };

        var metrics = new Dictionary<string, double>
        {
            { "LoadTime", 1.42 },
            { "ClickCount", 17 }
        };

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackEvent(
            "EventWithPropertiesAndMetrics",
            properties, metrics);

        await VerifyTrackMethod(ClientConsumer, "expected-event-with-properties-and-metrics.json");
    }

    [TestMethod]
    public async Task TrackEventWithNullTelemetryObject()
    {
        EventTelemetry noEvent = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackEvent(noEvent);

        await VerifyTrackMethod(ClientConsumer, "expected-event-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackEventWithSimpleEventTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var anEventTelemetry = new EventTelemetry("SimpleTestEvent");
            telemetryClient.TrackEvent(anEventTelemetry);
        }

        await VerifyTrackMethod(ClientConsumer, "expected-event-with-simple-event-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackEventWithEventTelemetryObject
        ()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
            => telemetryClient.TrackEvent(AnEventTelemetry());

        await VerifyTrackMethod(ClientConsumer, "expected-event-with-event-telemetry-object.json",
            InternalNodeNameShouldBeProvidedInTags, NodeNameShouldBeEqualTo(NodeName));
    }

    [TestMethod]
    public async Task TrackTrace()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Application Insights trace");

        var expectedJson = SelectExpectedJson("expected-trace.json", "expected-trace-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    private string SelectExpectedJson(string historicalJson, string otelJson)
    {
        if (IsNetCore8OrHigher())
        {
            if (_testDebug)
            {
                Console.WriteLine(otelJson + " used for " + FindDotNetEnv());
            }
            
            return otelJson;
        }
        
        if (_testDebug)
        {
            Console.WriteLine(historicalJson + " used for " + FindDotNetEnv());
        }
        
        return historicalJson;
    }
    private bool IsNetCore8OrHigher()
    {
        var framework = RuntimeInformation.FrameworkDescription;
        
        if (framework.StartsWith(".NET Framework"))
        {
            return false;
        }
        
        var version = Environment.Version;
        return version.Major >= 8;
    } 
    
    [TestMethod]
    public async Task TrackTraceWithSeverityLevel()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Application Insights trace", SeverityLevel.Warning);

        await VerifyTrackMethod(ClientConsumer, "expected-trace-with-severity-level.json");
    }

    [TestMethod]
    public async Task TrackTraceWithProperties()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };
            telemetryClient.TrackTrace("Application Insights trace",
                properties);
        }

        await VerifyTrackMethod(ClientConsumer, "expected-trace-with-properties.json");
    }

    [TestMethod]
    public async Task TrackTraceWithSeverityLevelAndProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace("Application Insights trace", SeverityLevel.Error, properties);

        await VerifyTrackMethod(ClientConsumer, "expected-trace-with-severity-level-and-properties.json");
    }


    [TestMethod]
    public async Task TrackTraceWithNullTelemetryObject()
    {
        TraceTelemetry noTrace = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackTrace(noTrace);

        await VerifyTrackMethod(ClientConsumer, "expected-trace-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackTraceWithSimpleTraceTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            var aTraceTelemetry = new TraceTelemetry("Application Insights trace", SeverityLevel.Critical);
            telemetryClient.TrackTrace(aTraceTelemetry);
        }

        await VerifyTrackMethod(ClientConsumer, "expected-trace-with-simple-trace-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackTraceWithTraceTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackTrace(ATraceTelemetry());

        await VerifyTrackMethod(ClientConsumer, "expected-trace-with-trace-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackMetric()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric("testMetric", 23.56);

        await VerifyTrackMethod(ClientConsumer, "expected-metric.json");
    }

    [TestMethod]
    public async Task TrackMetricWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackMetric("testMetricWithProperties", 37.26, properties);

        await VerifyTrackMethod(ClientConsumer, "expected-metric-with-properties.json");
    }

    [TestMethod]
    public async Task TrackMetricWithNullTelemetryObject()
    {
        MetricTelemetry noMetric = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackMetric(noMetric);

        await VerifyTrackMethod(ClientConsumer, "expected-metric-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackMetricWithSimpleMetricTelemetryObject()
    {
        var simpleMetricTelemetry = new MetricTelemetry("testMetricWithSimpleMetricTelemetryObject", 12.249);

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric(simpleMetricTelemetry);

        await VerifyTrackMethod(ClientConsumer, "expected-metric-with-simple-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackMetricWithTelemetryObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackMetric(AMetricTelemetry());

        await VerifyTrackMethod(ClientConsumer, "expected-metric-with-telemetry-object.json",
            NodeNameShouldBeEqualTo(NodeName));
    }

    [TestMethod]
    public async Task TrackException()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"));

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception.json", IdShouldBeProvidedInExceptions);
    }

    [TestMethod]
    public async Task TrackExceptionWithNullException()
    {
        Exception noException = null;

        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(noException);

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception-with-null-exception.json",
            IdShouldBeProvidedInExceptions);
    }

    [TestMethod]
    public async Task TrackExceptionWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"), properties);

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception-with-properties.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithPropertiesAndMetrics()
    {
        var properties = new Dictionary<string, string>
        {
            { "Category", "Error" },
            { "Source", "ExceptionTest" }
        };

        var metrics = new Dictionary<string, double>
        {
            { "RetryCount", 2 },
            { "DurationMs", 534 }
        };

        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"), properties, metrics);

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception-with-properties-and-metrics.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithNullTelemetryObject()
    {
        ExceptionTelemetry noException = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(noException);

        await VerifyTrackMethod(ClientConsumer, "expected-exception-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithSimpleExceptionTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient)
        {
            var exceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException("Something went wrong"));
            telemetryClient.TrackException(exceptionTelemetry);
        }

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithExceptionTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackException(AnExceptionTelemetry()
        );

        await VerifyTrackMethod(ExceptionConsumer, "expected-exception-with-exception-telemetry-object.json",
            NodeNameShouldBeEqualTo(NodeName));
    }

    [TestMethod]
    public async Task TrackRequest()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackRequest("GET /api/orders", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), "200",
                true);

        await VerifyTrackMethod(ClientConsumer, "expected-request.json", IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackRequestWithNullTelemetryObject()
    {
        RequestTelemetry noRequest = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackRequest(noRequest);

        await VerifyTrackMethod(ClientConsumer, "expected-request-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackRequestWithRequestTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackRequest(ARequestTelemetry());

        await VerifyTrackMethod(ExceptionConsumer, "expected-request-with-request-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackDependency()
    {
        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackDependency("SQL", "GetOrders", "SELECT * FROM Orders", DateTimeOffset.Now,
                TimeSpan.FromMilliseconds(123), true);

        await VerifyTrackMethod(ClientConsumer, "expected-dependency.json", IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackDependencyWithSeveralMethodArguments
        ()
    {
        var dependencyTypeName = "SQL";
        var target = "my-database";
        var dependencyName = "SELECT";
        var data = "SELECT * FROM Table";
        var startTime = DateTimeOffset.UtcNow.AddSeconds(-5);
        var duration = TimeSpan.FromSeconds(5);
        var resultCode = "2";
        var success = true;

        await VerifyTrackMethod(
            c => c.TrackDependency(dependencyTypeName, target, dependencyName, data, startTime, duration, resultCode,
                success),
            "expected-dependency-with-several-method-arguments.json"
        );
    }

    [TestMethod]
    public async Task TrackDependencyWithNullTelemetryObject()
    {
        DependencyTelemetry noDependency = null;

        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackDependency(noDependency);

        await VerifyTrackMethod(ClientConsumer, "expected-dependency-with-null-telemetry-object.json",
            IdShouldBeProvidedInBaseData);
    }

    [TestMethod]
    public async Task TrackDependencyWithTelemetryDependencyObject()
    {
        void ClientConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackDependency(ADependencyTelemetry());

        await VerifyTrackMethod(ClientConsumer, "expected-dependency-with-telemetry-dependency-object.json",
            IdShouldBeProvidedInBaseData,
            NodeNameShouldBeEqualTo(NodeName));
    }

    private static Action<JObject> NodeNameShouldBeEqualTo(string nodeName)
    {
        return json => { Assert.AreEqual(nodeName, (string)json["tags"]?["ai.internal.nodeName"]); };
    }

    [TestMethod]
    public async Task ContextProperties()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            telemetryClient.Context.Properties["Key1"] = "Value1";
            telemetryClient.Context.Properties["Key2"] = "Value2";
            telemetryClient.TrackTrace("Application Insights trace");
        }

        var expectedJson = SelectExpectedJson("expected-properties.json", "expected-properties-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    [TestMethod]
    public async Task ContextGlobalProperties()
    {
        void ClientConsumer(TelemetryClient telemetryClient)
        {
            telemetryClient.Context.GlobalProperties["global-Key1"] = "global-Value1";
            telemetryClient.Context.GlobalProperties["global-Key2"] = "global-Value2";
            telemetryClient.TrackTrace("Application Insights trace");
        }

        var expectedJson =
            SelectExpectedJson("expected-global-properties.json", "expected-global-properties-otel.json");
        await VerifyTrackMethod(ClientConsumer, expectedJson);
    }

    [TestMethod] // Longer to execute than other tests
    public async Task ShouldDisableTelemetry()
    {
        if (IsV452OrV6())
        {
            return;
        }

        // Arrange
        TelemetryConfiguration configuration = new TelemetryConfiguration();
        configuration.ConnectionString = _testConnectionString;

        configuration.DisableTelemetry = true; // Disable telemetry

        TelemetryClient telemetryClient = new TelemetryClient(configuration);

        // Act
        telemetryClient.TrackTrace("Application Insights trace");
        telemetryClient.Flush();

        // Assert
        var telemetryRequests = await FindRequestsOfTrackEndpoint();
        Assert.AreEqual(0, telemetryRequests.Count());
    }


    [TestMethod]
    public void ShouldNotFailIfNoConfiguration()
    {
        if (IsV452OrV6())
        {
            return;
        }

        // Arrange
        TelemetryConfiguration noConfiguration = null;

        TelemetryClient telemetryClient = new TelemetryClient(noConfiguration);

        // Act
        telemetryClient.TrackTrace("Application Insights trace");
        telemetryClient.Flush();
    }
    
    private static DependencyTelemetry ADependencyTelemetry()
    {
        var dependencyTelemetry = new DependencyTelemetry
        {
            Id = "dep-12345",
            Name = "SQL GetOrders",
            Target = "sqlserver01.database.windows.net",
            Data = "SELECT * FROM Orders",
            Type = "SQL",
            Duration = TimeSpan.FromMilliseconds(150),
            Timestamp = DateTimeOffset.UtcNow,
            Success = true,
            ResultCode = "0",
            CommandName = "ExecuteReader",
            Properties = { { "Key1", "Value1" }, { "Key2", "Value2" } }
        };

        dependencyTelemetry.Metrics["Metric1"] = 42.0;
        dependencyTelemetry.Metrics["Metric2"] = 3.14;

        dependencyTelemetry.Context.User.Id = "user-001";
        dependencyTelemetry.Context.User.AccountId = "account-abc";
        dependencyTelemetry.Context.Session.Id = "session-xyz";
        dependencyTelemetry.Context.Device.Id = "device-555";
        dependencyTelemetry.Context.Device.Model = "Surface Pro";
        dependencyTelemetry.Context.Device.Type = "PC";
        dependencyTelemetry.Context.Device.OemName = "Microsoft";
        dependencyTelemetry.Context.Device.OperatingSystem = "Windows 10";
        dependencyTelemetry.Context.Device.Language = "fr-FR";
        dependencyTelemetry.Context.Location.Ip = "192.0.2.1";
        dependencyTelemetry.Context.Cloud.RoleName = "api";
        dependencyTelemetry.Context.Cloud.RoleInstance = "instance-01";
        dependencyTelemetry.Context.Component.Version = "2.1.0";
        dependencyTelemetry.Context.Operation.Id = "op-req-01";
        dependencyTelemetry.Context.Operation.Name = "SampleOperation";
        dependencyTelemetry.Context.Operation.ParentId = "op-root-01";
        dependencyTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        dependencyTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        dependencyTelemetry.Context.Internal.NodeName = NodeName;
        dependencyTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";

        return dependencyTelemetry;
    }

    public TelemetryClientHttpMockTest()
    {
        if (IsV452OrV6())
        {
            var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
            Console.WriteLine("Test skipped for " + FindDotNetEnv());
            return;
        }

        _mockServer = WireMockServer.Start();

        _testConnectionString =
            $"InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint={_mockServer.Url}/v2.1/track;LiveEndpoint={_mockServer.Url}/live";

        _mockServer
            .Given(Request.Create()
                .WithPath("/v2.1/track")
                .UsingPost());
    }

    //  Azure.Monitor.OpenTelemetry.Exporter 1.4.0 and recent WireMock.Net are not compatible with .net framework 4.5.2 and 4.6.
    private static bool IsV452OrV6()
    {
        var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        return frameworkName.Contains("v4.5.2") || frameworkName.Contains("v4.6");
    }

    private static EventTelemetry AnEventTelemetry()
    {
        var eventTelemetry = new EventTelemetry
        {
            Name = "SampleEvent",
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "sequence123"
        };

        eventTelemetry.Properties["Category"] = "UserActions";
        eventTelemetry.Properties["Source"] = "MobileApp";

        eventTelemetry.Metrics["LoadTime"] = 1.42;
        eventTelemetry.Metrics["ClickCount"] = 17;

        eventTelemetry.Context.User.Id = "user-001";
        eventTelemetry.Context.User.AccountId = "account-abc";
        eventTelemetry.Context.Session.Id = "session-xyz";
        eventTelemetry.Context.Device.Id = "device-555";
        eventTelemetry.Context.Device.Model = "Pixel 8";
        eventTelemetry.Context.Device.Type = "Phone";
        eventTelemetry.Context.Device.OemName = "Google";
        eventTelemetry.Context.Device.OperatingSystem = "Android 14";
        eventTelemetry.Context.Device.Language = "en-US";
        eventTelemetry.Context.Location.Ip = "192.0.2.1";
        eventTelemetry.Context.Cloud.RoleName = "api";
        eventTelemetry.Context.Cloud.RoleInstance = "instance-01";
        eventTelemetry.Context.Component.Version = "2.1.0";
        eventTelemetry.Context.Operation.Id = "op-req-01";
        eventTelemetry.Context.Operation.Name = "SampleOperation";
        eventTelemetry.Context.Operation.ParentId = "op-root-01";
        eventTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        eventTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";
        eventTelemetry.Context.Internal.NodeName = NodeName;
        eventTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        return eventTelemetry;
    }

    private static TraceTelemetry ATraceTelemetry()
    {
        var traceTelemetry = new TraceTelemetry
        {
            Message = "Application Insights trace",
            SeverityLevel = SeverityLevel.Error,
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "seq-001"
        };

        traceTelemetry.Properties["Key1"] = "Value1";
        traceTelemetry.Properties["Key2"] = "Value2";
        traceTelemetry.Properties["environment"] = "test";

        traceTelemetry.Context.InstrumentationKey = "12345678-1234-1234-1234-123456789012";
        traceTelemetry.Context.Component.Version = "1.2.3";
        traceTelemetry.Context.User.Id = "user-xyz";
        traceTelemetry.Context.User.AccountId = "acc-abc";
        traceTelemetry.Context.Session.Id = "session-9876";
        traceTelemetry.Context.Device.Id = "device-42";
        traceTelemetry.Context.Device.Type = "PC";
        traceTelemetry.Context.Device.Model = "Surface Pro";
        traceTelemetry.Context.Device.OperatingSystem = "Windows 11";
        traceTelemetry.Context.Device.OemName = "Microsoft";
        traceTelemetry.Context.Device.Language = "en-US";
        traceTelemetry.Context.Location.Ip = "203.0.113.42";
        traceTelemetry.Context.Cloud.RoleName = "webapi";
        traceTelemetry.Context.Cloud.RoleInstance = "web01";
        traceTelemetry.Context.Operation.Id = "operation-001";
        traceTelemetry.Context.Operation.Name = "SampleOperation";
        traceTelemetry.Context.Operation.ParentId = "root-operation-id";
        traceTelemetry.Context.Operation.CorrelationVector = "cv-0:b";
        traceTelemetry.Context.Internal.SdkVersion = "customSdk-1.0.0";
        return traceTelemetry;
    }

    private static ExceptionTelemetry AnExceptionTelemetry()
    {
        var exceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException("New exception"))
        {
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "seq-001",
            SeverityLevel = SeverityLevel.Critical,
            ProblemId = "Problem-123",
            HandledAt = ExceptionHandledAt.UserCode,
            Message = "Excetion message",
        };

        exceptionTelemetry.Properties["Key1"] = "Value1";
        exceptionTelemetry.Properties["Key2"] = "Value2";

        exceptionTelemetry.Metrics["Metric1"] = 42.0;
        exceptionTelemetry.Metrics["Metric2"] = 3.14;

        exceptionTelemetry.Context.User.Id = "user-001";
        exceptionTelemetry.Context.User.AccountId = "account-abc";
        exceptionTelemetry.Context.Session.Id = "session-xyz";
        exceptionTelemetry.Context.Device.Id = "device-555";
        exceptionTelemetry.Context.Device.Model = "Surface Pro";
        exceptionTelemetry.Context.Device.Type = "PC";
        exceptionTelemetry.Context.Device.OemName = "Microsoft";
        exceptionTelemetry.Context.Device.OperatingSystem = "Windows 11";
        exceptionTelemetry.Context.Device.Language = "fr-FR";
        exceptionTelemetry.Context.Location.Ip = "192.0.2.1";
        exceptionTelemetry.Context.Cloud.RoleName = "api";
        exceptionTelemetry.Context.Cloud.RoleInstance = "instance-01";
        exceptionTelemetry.Context.Component.Version = "2.1.0";
        exceptionTelemetry.Context.Operation.Id = "op-req-01";
        exceptionTelemetry.Context.Operation.Name = "SampleOperation";
        exceptionTelemetry.Context.Operation.ParentId = "op-root-01";
        exceptionTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        exceptionTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        exceptionTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";
        exceptionTelemetry.Context.Internal.NodeName = NodeName;
        return exceptionTelemetry;
    }

    private static MetricTelemetry AMetricTelemetry()
    {
        var metricTelemetry = new MetricTelemetry
        {
            Name = "CustomMetric",
            Sum = 123.45,
            Min = 120,
            Max = 130,
            Count = 5,
            StandardDeviation = 3.2,
            Timestamp = DateTimeOffset.UtcNow,
            Sequence = "seq-123"
        };

        metricTelemetry.Properties["Key1"] = "Value1";
        metricTelemetry.Properties["Key2"] = "Value2";

        metricTelemetry.Context.InstrumentationKey = "12345678-1234-1234-1234-123456789012";
        metricTelemetry.Context.Component.Version = "3.2.1";
        metricTelemetry.Context.User.Id = "user-xyz";
        metricTelemetry.Context.User.AccountId = "account-abcd";
        metricTelemetry.Context.Session.Id = "session-5555";
        metricTelemetry.Context.Device.Id = "device-007";
        metricTelemetry.Context.Device.Type = "Desktop";
        metricTelemetry.Context.Device.Model = "Precision";
        metricTelemetry.Context.Device.OperatingSystem = "Windows 11";
        metricTelemetry.Context.Device.OemName = "Dell";
        metricTelemetry.Context.Device.Language = "en-US";
        metricTelemetry.Context.Location.Ip = "198.51.100.5";
        metricTelemetry.Context.Cloud.RoleName = "backend";
        metricTelemetry.Context.Cloud.RoleInstance = "service01";
        metricTelemetry.Context.Operation.Id = "op-4321";
        metricTelemetry.Context.Operation.Name = "MetricOperation";
        metricTelemetry.Context.Operation.ParentId = "op-root";
        metricTelemetry.Context.Operation.CorrelationVector = "cv-0:a";
        metricTelemetry.Context.Internal.SdkVersion = "netcoreapp3.1:2.206.0";
        metricTelemetry.Context.Internal.NodeName = NodeName;
        return metricTelemetry;
    }

    private static RequestTelemetry ARequestTelemetry()
    {
        var requestTelemetry = new RequestTelemetry
        {
            Id = "req-12345",
            Name = "GET /api/orders",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.FromMilliseconds(250),
            ResponseCode = "200",
            Success = true,
            Url = new Uri("https://api.example.com/api/orders"),
            Source = "frontend-service",
            Sequence = "seq-001",
            HttpMethod = "GET"
        };

        requestTelemetry.Properties["Key1"] = "Value1";
        requestTelemetry.Properties["Key2"] = "Value2";

        requestTelemetry.Metrics["Metric1"] = 42.0;
        requestTelemetry.Metrics["Metric2"] = 3.14;

        requestTelemetry.Context.User.Id = "user-001";
        requestTelemetry.Context.User.AccountId = "account-abc";
        requestTelemetry.Context.Session.Id = "session-xyz";
        requestTelemetry.Context.Device.Id = "device-555";
        requestTelemetry.Context.Device.Model = "Surface Pro";
        requestTelemetry.Context.Device.Type = "PC";
        requestTelemetry.Context.Device.OemName = "Microsoft";
        requestTelemetry.Context.Device.OperatingSystem = "Windows 11";
        requestTelemetry.Context.Device.Language = "fr-FR";
        requestTelemetry.Context.Location.Ip = "192.0.2.1";
        requestTelemetry.Context.Cloud.RoleName = "api";
        requestTelemetry.Context.Cloud.RoleInstance = "instance-01";
        requestTelemetry.Context.Component.Version = "2.1.0";
        requestTelemetry.Context.Operation.Id = "op-req-01";
        requestTelemetry.Context.Operation.Name = "SampleOperation";
        requestTelemetry.Context.Operation.ParentId = "op-root-01";
        requestTelemetry.Context.Operation.CorrelationVector = "cvector-001";
        requestTelemetry.Context.Internal.SdkVersion = "dotnet:2.18.0";
        requestTelemetry.Context.InstrumentationKey = "00000000-0000-0000-0000-000000000000";

        return requestTelemetry;
    }

    private async Task VerifyTrackMethod(Action<TelemetryClient> clientConsumer, string expectedFileName,
        params Action<JObject>[] assertions
    )
    {
        if (IsV452OrV6())
        {
            return;
        }

        // Arrange
        TelemetryConfiguration configuration = new TelemetryConfiguration();

        configuration.ConnectionString = _testConnectionString;

        TelemetryClient client = new TelemetryClient(configuration);

        // Act
        clientConsumer(client);
        client.Flush();

        var telemetryRequests = await FindRequestsOfTrackEndpoint();

        if (_testDebug)
        {
            PrintTrackHttpRequests(telemetryRequests);
        }

        // Assert
        Assert.AreEqual(1, telemetryRequests.Count(),
            "Should have found one HTTP telemetry request for " + FindDotNetEnv());

        var telemetryRequest = telemetryRequests.First();

        VerifyTrackHttpRequests(telemetryRequest.RequestMessage.Body, Path.Combine("json",
            expectedFileName), assertions);
    }

    private async Task<IEnumerable<ILogEntry>> FindRequestsOfTrackEndpoint()
    {
        var pollInterval = TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var timeout = TimeSpan.FromSeconds(10);

        while (stopwatch.Elapsed < timeout)
        {
            var requests = _mockServer.LogEntries;
            var telemetryRequests = requests.Where(r =>
                r.RequestMessage.Path.Contains("track")).ToList();

            if (telemetryRequests.Any())
            {
                return telemetryRequests;
            }

            await Task.Delay(pollInterval);
        }


        return _mockServer.LogEntries;
    }

    private static void PrintTrackHttpRequests(IEnumerable<ILogEntry> telemetryRequests)
    {
        foreach (var request in telemetryRequests)
        {
            if (request.RequestMessage.Body != null)
                Console.WriteLine("HTTP track requests sent: " + Environment.NewLine +
                                  JToken.Parse(request.RequestMessage.Body).ToString(Formatting.Indented));
        }
    }

    private static void VerifyTrackHttpRequests(string current, string expectedFileName,
        IEnumerable<Action<JObject>> assertions)
    {
        var expectedAsString = ReadFileAsString(expectedFileName);

        var currentJson = JObject.Parse(current);
        var expectedJSon = JObject.Parse(expectedAsString);

        TimeShouldBeProvided(currentJson);
        SdkVersionShouldBeProvided(currentJson);

        if (assertions != null)
        {
            foreach (var assertion in assertions)
            {
                assertion.Invoke(currentJson);
            }
        }

        RemoveNonComparableProperties(currentJson, expectedJSon);

        var frameworkName = FindDotNetEnv();
        var message =
            $"Expected ({expectedFileName}, {frameworkName}) {expectedJSon.ToString(Formatting.Indented)}\nActual: {currentJson.ToString(Formatting.Indented)}";
        Assert.IsTrue(JToken.DeepEquals(expectedJSon, currentJson), message);
    }

    private static string FindDotNetEnv()
    {
        var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        var version = Environment.Version;
        return ".Net env: " + Environment.NewLine + "* " + frameworkName + Environment.NewLine + "* " + version;
    }

    private static void IdShouldBeProvidedInBaseData(JObject currentJson)
    {
        var baseData = currentJson["data"]?["baseData"] as JObject;

        if (baseData == null)
            throw new AssertFailedException("Should have baseData property.");

        Assert.IsFalse(string.IsNullOrEmpty(baseData["id"]?.ToString()), "BaseData should have an id");
    }

    public static void IdShouldBeProvidedInExceptions(JObject currentJson)
    {
        var data = currentJson["data"] as JObject;

        if (data == null)
            throw new AssertFailedException("Should have data property.");

        var baseData = data["baseData"];

        if (baseData == null)
            throw new AssertFailedException("Should have baseData property.");

        var exceptions = baseData["exceptions"];
        if (exceptions == null)
            throw new AssertFailedException("Should have exceptions property.");

        Assert.AreEqual(1, exceptions.Count(), "Should have exactly one exception.");

        var exception = exceptions[0];

        Assert.IsFalse(string.IsNullOrEmpty(exception["id"]?.ToString()),
            "exceptions should contain id");
    }

    private static void InternalNodeNameShouldBeProvidedInTags(JObject currentJson)
    {
        var tags = currentJson["tags"] as JObject;

        if (tags == null)
            throw new AssertFailedException("Should have tags property.");

        Assert.IsFalse(string.IsNullOrEmpty(tags["ai.internal.nodeName"]?.ToString()),
            "tags should contain ai.internal.nodeName");
    }

    private static void RemoveNonComparableProperties(JObject currentJson, JObject expectedJSon)
    {
        currentJson.Remove("time");
        expectedJSon.Remove("time");
        RemoveSomeTagsProperties(currentJson);
        RemoveSomeTagsProperties(expectedJSon);
        RemoveIdFromBaseData(currentJson);
        RemoveIdFromBaseData(expectedJSon);
        RemoveExceptionId(currentJson);
        RemoveExceptionId(expectedJSon);
    }

    private static void RemoveSomeTagsProperties(JObject json)
    {
        if (json["tags"] is not JObject tags) return;
        tags.Remove("ai.cloud.role");
        tags.Remove("ai.cloud.roleInstance");
        tags.Remove("ai.internal.sdkVersion");
        tags.Remove("ai.internal.nodeName");
    }

    private static void RemoveIdFromBaseData(JObject json)
    {
        if (json["data"]?["baseData"] is not JObject baseData) return;
        baseData.Remove("id");
    }

    private static void RemoveExceptionId(JObject json)
    {
        var exceptions = json["data"]?["baseData"]?["exceptions"] as JArray;

        if (exceptions == null) return;

        foreach (var exception in exceptions)
        {
            if (exception is JObject exceptionObject)
            {
                exceptionObject.Remove("id");
            }
        }
    }

    private static void SdkVersionShouldBeProvided(JObject currentJson)
    {
        var tagsToken = currentJson["tags"];
        var sdkVersionToken = tagsToken["ai.internal.sdkVersion"];
        var sdkVersionValue = sdkVersionToken?.ToString();
        Assert.IsFalse(string.IsNullOrEmpty(sdkVersionValue), "ai.internal.sdkVersion must not be null or empty");
    }

    private static void TimeShouldBeProvided(JObject currentJson)
    {
        var timeValue = currentJson["time"];
        Assert.IsFalse(string.IsNullOrEmpty(timeValue?.ToString()), "Time field must not be null or empty");
    }

    private static string ReadFileAsString(string file, [CallerFilePath] string filePath = "")
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        var fullPath = Path.Combine(directoryPath ?? string.Empty, file);
        return File.ReadAllText(fullPath);
    }

    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }
}