namespace Microsoft.ApplicationInsights.AspNetCore
{ 
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        internal const string VersionPrefix = "aspnetCore-";

        internal static string GetFrameworkType()
        {
            string framework;
#if NET451
           // F representing the full framework
           framework = "F:"; 
#elif NETSTANDARD1_5
            // C representing the core framework
            framework = "C:";
#else
          // O representing other framework
          framework = "O:";
#endif
            return framework;
        }

        internal static string GetAssemblyVersion()
        {
            return typeof(SdkVersionUtils).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                      .First()
                      .InformationalVersion;
        }
    }
}
