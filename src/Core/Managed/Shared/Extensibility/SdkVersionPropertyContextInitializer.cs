namespace Microsoft.ApplicationInsights.Extensibility
{
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Initializes SDK Properties: SDK Version and SDKMode.
    /// </summary>
    internal sealed class SdkVersionPropertyContextInitializer : IContextInitializer
    {
        private const string SDKVersion = "SDKVersion";
        private string sdkVersion;

        /// <summary>
        /// Adds a telemetry property for the version of SDK.
        /// </summary>
        public void Initialize(TelemetryContext context)
        {
            var version = LazyInitializer.EnsureInitialized(ref this.sdkVersion, this.GetAssemblyVersion);
            if (string.IsNullOrEmpty(context.Internal.SdkVersion))
            {
                context.Internal.SdkVersion = version;
            }
        }

        private string GetAssemblyVersion()
        {
#if !WINRT && !CORE_PCL
            return typeof(SdkVersionPropertyContextInitializer).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#else
            return typeof(SdkVersionPropertyContextInitializer).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#endif
        }
    }
}