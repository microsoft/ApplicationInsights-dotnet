using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Docker;

namespace Microsoft.ApplicationInsights.Extensibility
{
    public class DockerContextInitializer : ITelemetryInitializer
    {
        private readonly DockerContextPoller contextPoller;

        public DockerContextInitializer()
        {
            #if !(NET40 || NET45)
                return;
            #endif

            this.contextPoller = new DockerContextPoller(Implementation.Docker.Constants.AiSdkDirectory);
            this.contextPoller.Start();
        }

        public void Initialize(ITelemetry telemetry)
        {
            #if !(NET40 || NET45)
                return;
            #endif

            DockerContext dockerContext;
            if (this.contextPoller.Completed && (dockerContext = this.contextPoller.DockerContext) != null)
            {
                TelemetryContext telemetryContext = telemetry.Context;

                // We always set the device ID, which is the device that sent the event.
                // By default GUID value, will be replaced with the real host name.
                telemetryContext.Device.Id = dockerContext.HostName;

                // If telemetry already initialized with Docker properties, we don't override it.
                if (CheckIfTelemetryContextAlreadyInitialized(telemetryContext))
                {
                    return;
                }

                foreach (KeyValuePair<string, string> property in dockerContext.Properties)
                {
                    telemetryContext.Properties.Add(property.Key, property.Value);
                }
            }
        }

        private bool CheckIfTelemetryContextAlreadyInitialized(TelemetryContext telemetryContext)
        {
            string containerName = null;
            telemetryContext.Properties.TryGetValue(Implementation.Docker.Constants.DockerContainerNamePropertyName, out containerName);

            return containerName != null;
        }
    }
}
