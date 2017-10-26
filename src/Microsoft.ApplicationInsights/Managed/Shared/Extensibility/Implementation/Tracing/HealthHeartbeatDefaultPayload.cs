namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    internal class HealthHeartbeatDefaultPayload : IHealthHeartbeatPayloadExtension
    {
        public static readonly string[] DefaultFields = 
        {
            "runtimeFramework",
            "targetFramework",
            "appinsightsSdkVer"
        };

        private List<string> enabledProperties;

        public HealthHeartbeatDefaultPayload() : this(null)
        {
        }

        public HealthHeartbeatDefaultPayload(IEnumerable<string> disableFields)
        {
            this.SetEnabledProperties(disableFields);
        }

        public string Name => "HealthHeartbeat";

        public int CurrentUnhealthyCount
        {
            get { return 0; }
        }

        public IEnumerable<KeyValuePair<string, object>> GetPayloadProperties()
        {
            var payload = new Dictionary<string, object>();
            foreach (string fieldName in this.enabledProperties)
            {
                switch (fieldName)
                {
                    case "runtimeFramework":
                        payload.Add(fieldName, this.GetRuntimeFrameworkVer());
                        break;
                    case "targetFramework":
                        payload.Add(fieldName, this.GetTargetFrameworkVer());
                        break;
                    case "appinsightsSdkVer":
                        payload.Add(fieldName, this.GetAppInsightsSdkVer());
                        break;
                    default:
                        throw new NotImplementedException(string.Format(CultureInfo.CurrentCulture, "No default handler implemented for field named '{0}'.", fieldName));
                }
            }

            return payload;
        }

        private void SetEnabledProperties(IEnumerable<string> disabledFields)
        {
            if (disabledFields == null || disabledFields.Count() <= 0)
            {
                this.enabledProperties = DefaultFields.ToList();
            }
            else
            {
                this.enabledProperties = new List<string>();
                foreach (string fieldName in DefaultFields)
                {
                    if (!disabledFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                    {
                        this.enabledProperties.Add(fieldName);
                    }
                }
            }
        }

        private bool IsFieldEnabled(string fieldName)
        {
            return this.enabledProperties.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
        }

        private string GetTargetFrameworkVer()
        {
#if NET45
            return "net45";
#elif NET46
            return "net46";
#elif NETCORE
            return "netstandard1.3";
#else 
            return "Undefined";
#endif
        }

        private string GetAppInsightsSdkVer()
        {
            return SdkVersionUtils.GetSdkVersion(string.Empty);
        }

        private string GetRuntimeFrameworkVer()
        {
#if NET45
            return System.Environment.Version.ToString();
#elif NETCORE
            // this crazy slice of code came from here: https://github.com/dotnet/BenchmarkDotNet/issues/448
            // I believe we will have more to do here, it would seem this is not something that the .NET Core makes easy

            var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
            {
                return assemblyPath[netCoreAppIndex + 1];
            }
            return string.Empty;
#else
            // is there any other framework we want to handle? 
            return "unknown";
#endif
        }
    }
}
