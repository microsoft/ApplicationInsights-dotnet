namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class HealthHeartbeatDefaultPayload : IHealthHeartbeatPayloadExtension
    {
        public const string FieldRuntimeFrameworkVer = "runtimeFramework";
        public const string FieldTargetFramework = "targetFramework";
        public const string FieldAppInsightsSdkVer = "appinsightsSdkVer";

        public string EnabledProperties;

        public HealthHeartbeatDefaultPayload(string enabledProperties)
        {
            this.EnabledProperties = enabledProperties;
        }

        public string Name => "SDKHealthHeartbeat";

        public int CurrentUnhealthyCount
        {
            get { return 0; }
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
#else
            return "na";
            // TODO: How do we determine runtime framework in Net Core?
#endif
        }
    }
}
