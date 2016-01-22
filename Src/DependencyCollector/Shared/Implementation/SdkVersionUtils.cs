namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        internal static string GetAssemblyVersion()
        {
            return typeof(SdkVersionUtils).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
        }
    }
}
