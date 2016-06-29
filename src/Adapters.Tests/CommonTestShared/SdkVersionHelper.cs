namespace Microsoft.ApplicationInsights.CommonTestShared
{
    using System;
    using System.Linq;
    using System.Reflection;

    public static class SdkVersionHelper
    {
        public static string GetExpectedSdkVersion(Type assemblyType, string prefix)
        {
            string versonStr = Assembly.GetAssembly(assemblyType).GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
            string[] versionParts = new Version(versonStr).ToString().Split('.');

            return prefix + string.Join(".", versionParts[0], versionParts[1], versionParts[2]) + "-" + versionParts[3];
        }
    }
}
