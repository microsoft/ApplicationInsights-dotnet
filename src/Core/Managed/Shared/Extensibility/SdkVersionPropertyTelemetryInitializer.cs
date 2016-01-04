namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Channel;

    /// <summary>
    /// Initializes SDK Properties: SDK Version and SDKMode.
    /// </summary>
    internal sealed class SdkVersionPropertyTelemetryInitializer : ITelemetryInitializer
    {
        private const string SDKVersion = "SDKVersion";
        private string sdkVersion;

        /// <summary>
        /// Adds a telemetry property for the version of SDK.
        /// </summary>
        public void Initialize(ITelemetry telemetry)
        {
            var version = LazyInitializer.EnsureInitialized(ref this.sdkVersion, this.GetAssemblyVersion);
            if (string.IsNullOrEmpty(telemetry.Context.Internal.SdkVersion))
            {
                telemetry.Context.Internal.SdkVersion = version;
            }
        }

        private string GetAssemblyVersion()
        {
#if !CORE_PCL
            return typeof(SdkVersionPropertyTelemetryInitializer).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#else
            return typeof(SdkVersionPropertyTelemetryInitializer).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#endif
        }
    }
}