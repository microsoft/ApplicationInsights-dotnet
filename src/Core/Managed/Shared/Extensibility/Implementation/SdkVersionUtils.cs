namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        /// <summary>
        /// Builds a string representing file version of the assembly with added prefix
        /// in format prefix:major.minor-revision.
        /// </summary>
        /// <param name="versionPrefix">Prefix to add to version.</param>
        /// <returns>String representation of the version with prefix added.</returns>
        internal static string GetSdkVersion(string versionPrefix)
        {
#if !CORE_PCL
            string versionStr = typeof(TelemetryClient).Assembly.GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;

#else
            string versionStr = typeof(TelemetryClient).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
#endif

            Version version = new Version(versionStr);
            string postfix = version.Revision.ToString();
#if NET40
            postfix += "-fw4";
#endif

            return versionPrefix + version.ToString(3) + "-" + postfix;
        }
    }
}