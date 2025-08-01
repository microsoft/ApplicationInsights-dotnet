using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using WireMock.Logging;
using WireMock.Server;
using Formatting = Newtonsoft.Json.Formatting;
using Request = WireMock.RequestBuilders.Request;


namespace Microsoft.ApplicationInsights;

public abstract class AbstractTelemetryClientHttpMockTest : IDisposable
{
    protected const string NodeName = "node-01";

    private readonly WireMockServer _mockServer;
    protected readonly string _testConnectionString;

    public AbstractTelemetryClientHttpMockTest()
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
    

    protected async Task VerifyTrackMethod(Action<TelemetryClient> clientConsumer, string expectedFileName,
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

        var httpRequestsSent = FindHttpRequestsSent(telemetryRequests);
        
        Console.WriteLine("HTTP track requests sent: " + Environment.NewLine + httpRequestsSent);
        
        // Assert
        Assert.AreEqual(1, telemetryRequests.Count(),
            "Should have found one HTTP telemetry request for " + FindDotNetEnv());

        var telemetryRequest = telemetryRequests.First();

        VerifyTrackHttpRequests(httpRequestsSent, telemetryRequest.RequestMessage.Body, Path.Combine("json",
            expectedFileName), assertions);
    }

    protected async Task<IEnumerable<ILogEntry>> FindRequestsOfTrackEndpoint()
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

    private static string FindHttpRequestsSent(IEnumerable<ILogEntry> telemetryRequests)
    {
        string httpRequestsSent = "";

        foreach (var request in telemetryRequests)
        {
            if (request.RequestMessage.Body != null)
            {
                httpRequestsSent += JToken.Parse(request.RequestMessage.Body).ToString(Formatting.Indented);
            }
        }

        return httpRequestsSent;
    }

    private static void VerifyTrackHttpRequests(string httpRequestsSent, string current, string expectedFileName,
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
            $"Expected ({expectedFileName}, {frameworkName}) {expectedJSon.ToString(Formatting.Indented)}\nActual: {currentJson.ToString(Formatting.Indented)}\nHTTP requests sent: {httpRequestsSent}";
        Assert.IsTrue(JToken.DeepEquals(expectedJSon, currentJson), message);
    }

    private static string FindDotNetEnv()
    {
        var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        var version = Environment.Version;
        return ".Net env: " + Environment.NewLine + "* " + frameworkName + Environment.NewLine + "* " + version;
    }
    
    //  WireMock.Net does not seem compatible with .net framework 4.5.2 and 4.6.
    protected static bool IsV452OrV6()
    {
        var frameworkName = AppDomain.CurrentDomain.SetupInformation.TargetFrameworkName;
        return frameworkName.Contains("v4.5.2") || frameworkName.Contains("v4.6");
    }
    
     private static void RemoveNonComparableProperties(JObject currentJson, JObject expectedJSon)
    {
        currentJson.Remove("time");
        expectedJSon.Remove("time");
        RemoveSomeTags(currentJson);
        RemoveSomeTags(expectedJSon);
        RemoveIdFromBaseData(currentJson);
        RemoveIdFromBaseData(expectedJSon);
        RemoveExceptionId(currentJson);
        RemoveExceptionId(expectedJSon);
    }

    private static void RemoveSomeTags(JObject json)
    {
        if (json["tags"] is not JObject tags) return;
        tags.Remove("ai.cloud.role");
        tags.Remove("ai.cloud.roleInstance");
        tags.Remove("ai.internal.sdkVersion");
        tags.Remove("ai.internal.nodeName");
        tags.Remove("ai.operation.id");
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

    protected static void IdShouldBeProvidedInBaseData(JObject currentJson)
    {
        var baseData = currentJson["data"]?["baseData"] as JObject;

        if (baseData == null)
            throw new AssertFailedException("Should have baseData property.");

        Assert.IsFalse(string.IsNullOrEmpty(baseData["id"]?.ToString()), "BaseData should have an id");
    }
    private static string ReadFileAsString(string file, [CallerFilePath] string filePath = "")
    {
        var directoryPath = Path.GetDirectoryName(filePath);
        var fullPath = Path.Combine(directoryPath ?? string.Empty, file);
        return File.ReadAllText(fullPath);
    }

    protected static Action<JObject> NodeNameShouldBeEqualTo(string nodeName)
    {
        return json => { Assert.AreEqual(nodeName, (string)json["tags"]?["ai.internal.nodeName"]); };
    }
    
    public void Dispose()
    {
        _mockServer?.Stop();
        _mockServer?.Dispose();
    }
    
}