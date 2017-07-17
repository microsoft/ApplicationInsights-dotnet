namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
#if NET451 || NET46
        public const string VersionPrefix = "aspnet5f:";
#else
        public const string VersionPrefix = "aspnet5c:";
#endif

        internal static string GetAssemblyVersion()
        {
            return typeof(SdkVersionUtils).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                      .First()
                      .InformationalVersion;
        }
    }
}
