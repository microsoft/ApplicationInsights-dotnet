namespace Microsoft.ApplicationInsights.CommonTestShared
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class SdkVersionHelper
    {
        public static string GetExpectedSdkVersion(Type assemblyType, string prefix)
        {
#if NET45 || NET46
            string versionStr = typeof(SdkVersionHelper).Assembly.GetCustomAttributes(false).OfType<AssemblyFileVersionAttribute>().First().Version;
#else
            string versionStr = typeof(SdkVersionHelper).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>().First().Version;
#endif
            string[] versionParts = new Version(versionStr).ToString().Split('.');

            return prefix + string.Join(".", versionParts[0], versionParts[1], versionParts[2]) + "-" + versionParts[3];
        }
    }
}
