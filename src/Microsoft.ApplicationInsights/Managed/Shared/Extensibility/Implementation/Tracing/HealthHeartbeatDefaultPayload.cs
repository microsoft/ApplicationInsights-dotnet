namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class HealthHeartbeatDefaultPayload : IHealthHeartbeatPayloadExtension
    {
        public Enum DefaultFields
        {
            FieldRuntimeFrameworkVer = "runtimeFramework",
            FieldTargetFramework = "targetFramework",
            FieldAppInsightsSdkVer = "appinsightsSdkVer"
        };

        private string enabledProperties;

        public HealthHeartbeatDefaultPayload(string enabledProperties)
        {
            this.EnabledProperties = enabledProperties;
        }

        public string Name => "SDKHealthHeartbeat";

        public int CurrentUnhealthyCount
        {
            get { return 0; }
        }

        public string EnabledProperties
        {
            get => this.enabledProperties;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("EnabledProperties", "Cannot set enabled properties to null or the empty string. Set the catch-all value '*' or some valid string to capture actual properties with.");
                }

                this.enabledProperties = value;
            }
        }

        public IEnumerable<KeyValuePair<string, object>> GetPayloadProperties()
        {
            var payload = new Dictionary<string, object>();
            if (this.IsFieldEnabled(FieldTargetFramework))
            {
                payload.Add(FieldTargetFramework, this.GetTargetFrameworkVer());
            }

            if (this.IsFieldEnabled(FieldAppInsightsSdkVer))
            {
                payload.Add(FieldAppInsightsSdkVer, this.GetAppInsightsSdkVer());
            }

            if (this.IsFieldEnabled(FieldRuntimeFrameworkVer))
            {
                payload.Add(FieldRuntimeFrameworkVer, this.GetRuntimeFrameworkVer());
            }

            return payload;
        }

        private bool IsFieldEnabled(string fieldName)
        {
            if (!this.EnabledProperties.Equals("*", StringComparison.OrdinalIgnoreCase))
            {
                return this.EnabledProperties.IndexOf(fieldName, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return true;
        }

        private string GetTargetFrameworkVer()
        {
#if NET45
            return "net45";
#else
            return "netstandard1.3";
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
