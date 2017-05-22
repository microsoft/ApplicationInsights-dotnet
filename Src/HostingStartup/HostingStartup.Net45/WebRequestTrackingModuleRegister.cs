namespace Microsoft.ApplicationInsights.Extensibility.HostingStartup
{
    using System;
    using System.Reflection;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    /// <summary>
    /// Class for registering WebRequestTrackingModule.
    /// </summary>
    public class WebRequestTrackingModuleRegister
    {
        /// <summary>ApplicationInsights web assembly name.</summary>
        private const string ApplicationInisghtsAssemblyName = "Microsoft.AI.Web";

        /// <summary>ApplicationInsights web HTTP module name.</summary>
        private const string ApplicationInsightsModuleName = "Microsoft.ApplicationInsights.Web.ApplicationInsightsHttpModule";

        /// <summary>TelemetryCorrelation web assembly name.</summary>
        private const string TelemetryCorrelationAssemblyName = "Microsoft.AspNet.TelemetryCorrelation";

        /// <summary>TelemetryCorrelation web HTTP module name.</summary>
        private const string TelemetryCorrelationModuleName = "Microsoft.AspNet.TelemetryCorrelation.TelemetryCorrelationHttpModule";

        /// <summary>
        /// Gets the HTTP module type.
        /// </summary>
        /// <param name="assemblyName">Assembly name.</param>
        /// <param name="typeName">Type name.</param>
        /// <returns>The application insights HTTP module type.</returns>
        public static Type GetModuleType(string assemblyName, string typeName)
        {
            // First validate that the module can be loaded
            Type moduleType = null;

            HostingStartupEventSource.Log.HttpModuleLoadingStart(assemblyName, typeName);

            try
            {
                var assembly = Assembly.Load(assemblyName);
                if (assembly == null)
                {
                    HostingStartupEventSource.Log.HttpModuleLoadingError(assemblyName, typeName, "assembly cannot be found");
                    return null;
                }

                moduleType = assembly.GetType(typeName);

                if (moduleType == null)
                {
                    HostingStartupEventSource.Log.HttpModuleLoadingError(assemblyName, typeName, "type cannot be found");
                    return null;
                }

                var module = Activator.CreateInstance(moduleType);

                if (module == null)
                {
                    HostingStartupEventSource.Log.HttpModuleLoadingError(assemblyName, typeName, "module cannot be created");
                    return null;
                }
            }
            catch (Exception ex)
            {
                HostingStartupEventSource.Log.HttpModuleLoadingError(assemblyName, typeName, ex.ToString());
                return null;
            }

            HostingStartupEventSource.Log.HttpModuleLoadingEnd(assemblyName, typeName);

            return moduleType;
        }

        /// <summary>
        /// Registers WebRequestTrackingModule.
        /// </summary>
        public static void Register()
        {
            // loads telemetry correlation HTTP module
            var telemetryCorrelationModuleType = GetModuleType(TelemetryCorrelationAssemblyName, TelemetryCorrelationModuleName);

            if (telemetryCorrelationModuleType != null)
            {
                DynamicModuleUtility.RegisterModule(telemetryCorrelationModuleType);
            }

            // loads application insights HTTP module
            var applicationInsightsModuleType = GetModuleType(ApplicationInisghtsAssemblyName, ApplicationInsightsModuleName);

            if (applicationInsightsModuleType != null)
            {
                DynamicModuleUtility.RegisterModule(applicationInsightsModuleType);
            }
        }
    }
}
