using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights;

[TestClass]
public class TelemetryClientHttpMockTest : AbstractTelemetryClientHttpMockTest
{
    
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
    
}