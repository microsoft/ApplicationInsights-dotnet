using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.ApplicationInsights.Internals
{
    /// <summary>
    /// Containers helper functions for determining versions of assemblies.
    /// </summary>
    internal class VersionUtils
    {
        internal static string GetVersion(Type type)
        {
            string versionString = type
                    .Assembly
                    .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                    .First()
                    .InformationalVersion;

                // Informational version may contain extra information.
                // 1) "1.1.0-beta2+a25741030f05c60c85be102ce7c33f3899290d49". Ignoring part after '+' if it is present.
                // 2) "4.6.30411.01 @BuiltBy: XXXXXX @Branch: XXXXXX @srccode: XXXXXX XXXXXX" Ignoring part after '@' if it is present.
                string shortVersion = versionString.Split('+', '@', ' ')[0];
                return shortVersion;
        }
    }
}