using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
namespace Microsoft.ApplicationInsights;

[TestClass]
public class ExceptionTelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
     [TestMethod]
    public async Task TrackException()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"));

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception.json", IdShouldBeProvidedInExceptions);
    }

    [TestMethod]
    public async Task TrackExceptionWithNullException()
    {
        Exception noException = null;

        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(noException);

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception-with-null-exception.json",
            IdShouldBeProvidedInExceptions);
    }

    [TestMethod]
    public async Task TrackExceptionWithProperties()
    {
        var properties = new Dictionary<string, string> { { "Key1", "Value1" }, { "Key2", "Value2" } };

        void ExceptionConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(new InvalidOperationException("Something went wrong"), properties);

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception-with-properties.json");
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

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception-with-properties-and-metrics.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithNullTelemetryObject()
    {
        ExceptionTelemetry noException = null;

        void ClientConsumer(TelemetryClient telemetryClient) =>
            telemetryClient.TrackException(noException);

        await VerifyTrackMethod(ClientConsumer, "exception/expected-exception-with-null-telemetry-object.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithSimpleExceptionTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient)
        {
            var exceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException("Something went wrong"));
            telemetryClient.TrackException(exceptionTelemetry);
        }

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception.json");
    }

    [TestMethod]
    public async Task TrackExceptionWithExceptionTelemetryObject()
    {
        void ExceptionConsumer(TelemetryClient telemetryClient) => telemetryClient.TrackException(AnExceptionTelemetry()
        );

        await VerifyTrackMethod(ExceptionConsumer, "exception/expected-exception-with-exception-telemetry-object.json",
            NodeNameShouldBeEqualTo(NodeName));
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
    
}