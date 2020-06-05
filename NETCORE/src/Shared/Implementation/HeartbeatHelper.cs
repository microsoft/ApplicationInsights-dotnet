namespace Microsoft.ApplicationInsights.Shared.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Reflection;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.WindowsServer;

    /// <summary>
    /// Two of our modules depend on references to DiagnosticsTelemetryModule.
    /// In a classic net framework application, these modules will use the static TelemetryModules.Instance and set themselves.
    /// This static instance is not available in net core applications so we must set it manually.
    /// </summary>
    /// <remarks>
    /// We would like to make changes to Heartbeat at a later date so I don't want to add this field to the public api at this time.
    /// Because this uses reflection, I'm putting this is a separate class with unit tests so we can quickly identify any breaking changes.
    /// </remarks>
    internal static class HeartbeatHelper
    {
        /// <summary>
        /// Set the HeartbeatPropertyManager  on <see cref="AppServicesHeartbeatTelemetryModule"/>.
        /// </summary>
        /// <param name="module">Instance to set the field on.</param>
        /// <param name="heartbeatPropertyManager">Instance of <see cref="IHeartbeatPropertyManager"/>.</param>
        public static void SetHeartbeatPropertyManager(AppServicesHeartbeatTelemetryModule module, IHeartbeatPropertyManager heartbeatPropertyManager)
        {
            if (heartbeatPropertyManager != null)
            {
                Type appServicesHeartbeatTelemetryModuleType = typeof(AppServicesHeartbeatTelemetryModule);
                var property = appServicesHeartbeatTelemetryModuleType.GetProperty("HeartbeatPropertyManager", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(property != null, "Reflection failed, property not found.");
                property.SetValue(module, heartbeatPropertyManager);
            }
        }

        /// <summary>
        /// Set the HeartbeatPropertyManager  on <see cref="AzureInstanceMetadataTelemetryModule"/>.
        /// </summary>
        /// <param name="module">Instance to set the field on.</param>
        /// <param name="heartbeatPropertyManager">Instance of <see cref="IHeartbeatPropertyManager"/>.</param>
        public static void SetHeartbeatPropertyManager(AzureInstanceMetadataTelemetryModule module, IHeartbeatPropertyManager heartbeatPropertyManager)
        {
            if (heartbeatPropertyManager != null)
            {
                Type appServicesHeartbeatTelemetryModuleType = typeof(AzureInstanceMetadataTelemetryModule);
                var property = appServicesHeartbeatTelemetryModuleType.GetProperty("HeartbeatPropertyManager", BindingFlags.NonPublic | BindingFlags.Instance);
                Debug.Assert(property != null, "Reflection failed, property not found.");
                property.SetValue(module, heartbeatPropertyManager);
            }
        }
    }
}
