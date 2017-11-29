namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class HeartbeatDefaultPayload
    {
       public static readonly string[] DefaultFields =
        {
            "runtimeFramework",
            "baseSdkTargetFramework"
        };

        public static void PopulateDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatProvider provider)
        {
            var enabledProperties = RemoveDisabledDefaultFields(disabledFields);

            var payload = new Dictionary<string, HeartbeatPropertyPayload>();
            foreach (string fieldName in enabledProperties)
            {
                try
                {
                    switch (fieldName)
                    {
                        case "runtimeFramework":
                            provider.AddHealthProperty(fieldName, GetRuntimeFrameworkVer(), true);
                            break;
                        case "baseSdkTargetFramework":
                            provider.AddHealthProperty(fieldName, GetBaseSdkTargetFramework(), true);
                            break;
                        default:
                            provider.AddHealthProperty(fieldName, "UNDEFINED", false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.FailedToObtainDefaultHeartbeatProperty(fieldName, ex.ToString());
                }
            }
        }

        private static List<string> RemoveDisabledDefaultFields(IEnumerable<string> disabledFields)
        {
            List<string> enabledProperties = new List<string>();

            if (disabledFields == null || disabledFields.Count() <= 0)
            {
                enabledProperties = DefaultFields.ToList();
            }
            else
            {
                enabledProperties = new List<string>();
                foreach (string fieldName in DefaultFields)
                {
                    if (!disabledFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                    {
                        enabledProperties.Add(fieldName);
                    }
                }
            }

            return enabledProperties;
        }

        private static string GetBaseSdkTargetFramework()
        {
#if NET45
            return "net45";
#elif NET46
            return "net46";
#elif NETSTANDARD1_3
            return "netstandard1.3";
#else
#error Unrecognized framework
            return "undefined";
#endif
        }

        private static string GetRuntimeFrameworkVer()
        {
            Assembly assembly = typeof(Object).GetTypeInfo().Assembly;

            AssemblyFileVersionAttribute objectAssemblyFileVer =
                        assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                                .Cast<AssemblyFileVersionAttribute>()
                                .FirstOrDefault();

            return objectAssemblyFileVer != null ? objectAssemblyFileVer.Version : "undefined";
        }
    }
}
