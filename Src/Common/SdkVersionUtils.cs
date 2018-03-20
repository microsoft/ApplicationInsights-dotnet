namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
#if NETSTANDARD1_6
    using System.Collections.Generic;
#endif

    internal class SdkVersionUtils
    {
        internal static string GetSdkVersion(string versionPrefix)
        {
            // Since dependencySource is no longer set, sdk version is prepended with information which can identify whether RDD was collected by profiler/framework
            // For directly using TrackDependency(), version will be simply what is set by core
            Type sdkVersionUtilsType = typeof(SdkVersionUtils);

#if NETSTANDARD1_6
            IEnumerable<Attribute> assemblyCustomAttributes = sdkVersionUtilsType.GetTypeInfo().Assembly.GetCustomAttributes();
#else
            object[] assemblyCustomAttributes = sdkVersionUtilsType.Assembly.GetCustomAttributes(false);
#endif
            string versionStr = assemblyCustomAttributes
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;

            Version version = new Version(versionStr);

            string postfix = version.Revision.ToString(CultureInfo.InvariantCulture);
            return (versionPrefix ?? string.Empty) + version.ToString(3) + "-" + postfix;
        }
    }
}