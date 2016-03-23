namespace Microsoft.ApplicationInsights.AspNet
{ 
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        internal const string VersionPrefix = "aspnet5-";

        internal static string GetFrameworkType()
        {
            string framework;
#if dnx451
           // F representing the full framework
           framework = "F:"; 
#elif dnxcore50
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
