namespace Microsoft.ApplicationInsights.NLogTarget.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal static class SdkVersionHelper
    {
        public static string GetExpectedSdkVersion(string prefix, Type loggerType)
        {
#if NETFRAMEWORK
            var attributes = loggerType.Assembly.GetCustomAttributes(false);
#else
            var attributes = loggerType.GetTypeInfo().Assembly.GetCustomAttributes();
#endif
            var versionAttribute = attributes
                .OfType<AssemblyFileVersionAttribute>()
                .First();

            var version = new Version(versionAttribute.Version);
            return string.Concat(
                prefix,
                version.Major,
                ".",
                version.Minor,
                ".",
                version.Build,
                "-",
                version.Revision);
        }
    }
}
