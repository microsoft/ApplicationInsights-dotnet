namespace Microsoft.ApplicationInsights.AspNetCore
{ 
    using System.Linq;
    using System.Reflection;

    internal class SdkVersionUtils
    {
        internal static string GetFrameworkType()
        {
            string framework;
#if NET451
           // F representing the full framework
           framework = "aspnet5F:";
#else
            // C representing the core framework
            framework = "aspnet5C:";
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
