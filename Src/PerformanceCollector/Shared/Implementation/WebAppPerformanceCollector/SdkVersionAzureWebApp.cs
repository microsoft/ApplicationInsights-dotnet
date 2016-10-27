namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using Web.Implementation;

    internal class SdkVersionAzureWebApp
    {
        internal static string sdkVersionAzureWebApp = VersionPrefix + GetAssemblyVersion();

        internal const string VersionPrefix = "azwapc: ";

        internal static string GetAssemblyVersion()
        {
            return null;
            //return SdkVersionUtils.GetAssemblyVersion();
        }
    }
}