namespace Microsoft.ApplicationInsights.Internals
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Contains helper functions for determining versions of assemblies.
    /// </summary>
    internal class VersionUtils
    {
        /// <summary>
        /// Returns a human-readable version string for a specific type. If the version cannot be determined, returns "u".
        /// </summary>
        /// <param name="type">The type for which to get the version</param>
        /// <returns>String representation of the type's version</returns>
        internal static string GetVersion(Type type)
        {
            string versionString = type
                    .Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion ?? "u"; // if we can't get a version, "u" means unknown

            // Informational version may contain extra information.
            // 1) "1.1.0-beta2+a25741030f05c60c85be102ce7c33f3899290d49". Ignoring part after '+' if it is present.
            // 2) "4.6.30411.01 @BuiltBy: XXXXXX @Branch: XXXXXX @srccode: XXXXXX XXXXXX" Ignoring part after '@' if it is present.
            string shortVersion = versionString.Split('+', '@', ' ')[0];
            return shortVersion;
        }
    }
}
