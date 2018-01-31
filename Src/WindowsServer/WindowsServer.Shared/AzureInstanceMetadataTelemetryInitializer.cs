namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;

    /// <summary>
    /// A telemetry initializer that adds Azure specific context information to the heartbeat, if it is available.
    /// </summary>
    public class AzureInstanceMetadataTelemetryInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AzureInstanceMetadataTelemetryInitializer" /> class.
        /// Creates a heartbeat property collector that obtains and inserts data from the Azure Instance
        /// Metadata service if it is present and available to the currently running process. If it is not
        /// present no added IMS data is added to the heartbeat.
        /// </summary>
        public AzureInstanceMetadataTelemetryInitializer()
        {
            WindowsServerEventSource.Log.TelemetryInitializerLoaded(this.GetType().FullName);

            var telemetryModules = TelemetryModules.Instance;
            foreach (var module in telemetryModules.Modules)

            {
                if (module is IHeartbeatPropertyManager)
                {
                    var hbeatManager = (IHeartbeatPropertyManager)module;

                    // start off the heartbeat property collection process, but don't wait for it nor report
                    // any status from here. The thread running the collection will report to the core event log.
                    var heartbeatProperties = new AzureHeartbeatProperties();
                    Task.Factory.StartNew(
                        async () => await heartbeatProperties.SetDefaultPayload(hbeatManager.ExcludedHeartbeatProperties, hbeatManager)
                        .ConfigureAwait(false));                    
                }
            }
        }

        /// <summary>
        /// For this initializer nothing further needs to be done, as all the properties are cached within the heartbeat 
        /// provider itself.
        /// </summary>
        /// <param name="telemetry">The telemetry to initialize (unused in this instance).</param>
        public void Initialize(ITelemetry telemetry)
        {
            return;
        }
    }
}
