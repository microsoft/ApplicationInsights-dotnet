namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Collections.Generic;
    using Channel;
    using DataContracts;
    using Implementation.Docker;

    /// <summary>
    /// Initializes telemetries with Docker context, if available.
    /// Docker context is the host, image, container name and ID in which the application is running in.
    /// The Docker context file is injected by the Application Insights Monitoring Container:
    ///     https://github.com/Microsoft/ApplicationInsights-Docker
    /// </summary>
    public class DockerContextInitializer : ITelemetryInitializer
    {
        private readonly DockerContextPoller contextPoller;

        /// <summary>
        /// Constructs new DockerContextInitializer. This constructor is for testability purposes.
        /// </summary>
        /// <param name="dockerContextPoller">The context poller.</param>
        public DockerContextInitializer(DockerContextPoller dockerContextPoller)
        {
            this.contextPoller = dockerContextPoller;
        }

        /// <summary>
        /// Constructs new DockerContextInitializer.
        /// </summary>
        public DockerContextInitializer()
        {
            this.contextPoller = new DockerContextPoller(Implementation.Docker.Constants.AiSdkDirectory);
            this.contextPoller.Start();
        }

        public void Initialize(ITelemetry telemetry)
        {
            DockerContext dockerContext;
            if ((dockerContext = this.contextPoller.DockerContext) != null)
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

        private static bool CheckIfTelemetryContextAlreadyInitialized(TelemetryContext telemetryContext)
        {
            string containerName = null;
            telemetryContext.Properties.TryGetValue(Implementation.Docker.Constants.DockerContainerNamePropertyName, out containerName);

            return containerName != null;
        }
    }
}
