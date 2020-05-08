namespace Microsoft.ApplicationInsights.WindowsServer
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation;

    /// <summary>
    /// A telemetry module that adds Azure instance metadata context information to the heartbeat, if it is available.
    /// </summary>
    public sealed class AzureInstanceMetadataTelemetryModule : ITelemetryModule
    {
        // Cache the heartbeat property manager across updates. Note that tests can also override the heartbeat manager.
        internal IHeartbeatPropertyManager HeartbeatManager;

        private bool isInitialized = false;
        private object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureInstanceMetadataTelemetryModule" /> class.
        /// Creates a heartbeat property collector that obtains and inserts data from the Azure Instance
        /// Metadata service if it is present and available to the currently running process. If it is not
        /// present no added IMS data is added to the heartbeat.
        /// </summary>
        /// <param name="unused">Unused parameter for this TelemetryModule.</param>
        public void Initialize(TelemetryConfiguration unused)
        {
            // Core SDK creates 1 instance of a module but calls Initialize multiple times
            if (!this.isInitialized)
            {
                lock (this.lockObject)
                {
                    if (!this.isInitialized)
                    {
                        var telemetryModules = TelemetryModules.Instance; // TODO: THIS

                        var hbeatManager = this.GetHeartbeatPropertyManager();
                        if (hbeatManager != null)
                        {
                            // start off the heartbeat property collection process, but don't wait for it nor report
                            // any status from here, fire and forget. The thread running the collection will report 
                            // to the core event log.
                            try
                            {
                                var heartbeatProperties = new AzureComputeMetadataHeartbeatPropertyProvider();
                                Task.Factory.StartNew(
                                    async () => await heartbeatProperties.SetDefaultPayloadAsync(hbeatManager)
                                    .ConfigureAwait(false));
                            }
                            catch (Exception heartbeatAquisitionException)
                            {
                                WindowsServerEventSource.Log.AzureInstanceMetadataFailureWithException(heartbeatAquisitionException.Message, heartbeatAquisitionException.InnerException?.Message);
                            }
                        }

                        this.isInitialized = true;
                    }
                }
            }
        }

        private IHeartbeatPropertyManager GetHeartbeatPropertyManager()
        {
            if (this.HeartbeatManager == null)
            {
                // TODO: THIS CAUSES THE HEARTBEAT TEST TO FAIL BECAUSE IT'S LOOKING AT THE WRONG COLLECTION OF MODULES
                var telemetryModules = TelemetryModules.Instance;

                try
                {
                    foreach (var module in telemetryModules.Modules)
                    {
                        if (module is IHeartbeatPropertyManager hman)
                        {
                            this.HeartbeatManager = hman;
                        }
                    }
                }
                catch (Exception hearbeatManagerAccessException)
                {
                    // TODO: MISSING LOGGING HERE FOR AzureInstanceMetadataTelemetryModule
                    // WindowsServerEventSource.Log.AppServiceHeartbeatManagerAccessFailure(hearbeatManagerAccessException.ToInvariantString());
                }

                if (this.HeartbeatManager == null)
                {
                    // TODO: MISSING LOGGING HERE FOR AzureInstanceMetadataTelemetryModule
                    // WindowsServerEventSource.Log.AppServiceHeartbeatManagerNotAvailable();
                }
            }

            return this.HeartbeatManager;
        }
    }
}
