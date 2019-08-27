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

        /// <summary>
        /// Get the Assembly Version with SDK prefix.
        /// </summary>
        internal static string GetVersion()
        {
            return VersionPrefix + GetAssemblyVersion();
        }

        /// <summary>
        /// Get the Assembly Version with given SDK prefix.
        /// </summary>
        internal static string GetVersion(string versionPrefix)
        {
            return versionPrefix + GetAssemblyVersion();
        }

        private static string GetAssemblyVersion()
        {
            return typeof(SdkVersionUtils).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                      .First()
                      .InformationalVersion;
        }
    }
}
