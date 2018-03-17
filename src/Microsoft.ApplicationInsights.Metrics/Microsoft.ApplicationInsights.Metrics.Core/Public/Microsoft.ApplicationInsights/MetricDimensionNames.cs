using System;

using Microsoft.ApplicationInsights.Metrics;

namespace Microsoft.ApplicationInsights
{
    /// <summary>
    /// Contains constants used to refer to metric dimensions with special significance.
    /// </summary>
    public static class MetricDimensionNames
    {
        /// <summary>
        /// Contains constants used to refer to metric dimensions that will be mapped to fields
        /// within the <see cref="Microsoft.ApplicationInsights.DataContracts.TelemetryContext"/> attached to Application
        /// Insights metric telemetry that represents <see cref="MetricAggregate" /> objects sent to the Application Insights
        /// cloud ingestion endpoint.
        /// </summary>
        public static class TelemetryContext
        {
#pragma warning disable SA1202 // Elements must be ordered by access
            /// <summary></summary>
            public const string InstrumentationKey = TelemetryContextPrefix + "InstrumentationKey";

            private const string TelemetryContextPrefix = "TelemetryContext.";
            private const string PropertyPrefix = TelemetryContextPrefix + "Property_";
            private const string PropertyPostfix = "_";

            /// <summary>
            /// </summary>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            public static string Property(string propertyName)
            {
                Util.ValidateNotNullOrWhitespace(propertyName, nameof(propertyName));
                string dimName = PropertyPrefix + propertyName + PropertyPostfix;
                return dimName;
            }

            /// <summary>
            /// </summary>
            /// <param name="dimensionName"></param>
            /// <param name="propertyName"></param>
            /// <returns></returns>
            public static bool IsProperty(string dimensionName, out string propertyName)
            {
                propertyName = null;
                if (String.IsNullOrWhiteSpace(dimensionName))
                {
                    return false;
                }

                if (false == dimensionName.StartsWith(PropertyPrefix, StringComparison.Ordinal))
                {
                    return false;
                }

                if (false == dimensionName.EndsWith(PropertyPostfix, StringComparison.Ordinal))
                {
                    return false;
                }

                propertyName = dimensionName.Substring(PropertyPrefix.Length);
                propertyName = propertyName.Substring(0, propertyName.Length - PropertyPostfix.Length);
                return true;
            }

            /// <summary></summary>
            public static class Cloud
            {
                private const string CloudPrefix = TelemetryContextPrefix + "Cloud.";

                /// <summary></summary>
                public const string RoleInstance = CloudPrefix + "RoleInstance";

                /// <summary></summary>
                public const string RoleName = CloudPrefix + "RoleName";
            }

            /// <summary></summary>
            public static class Component
            {
                private const string ComponentPrefix = TelemetryContextPrefix + "Component.";

                /// <summary></summary>
                public const string Version = ComponentPrefix + "Version";
            }

            /// <summary></summary>
            public static class Device
            {
                private const string DevicePrefix = TelemetryContextPrefix + "Device.";

                /// <summary></summary>
                public const string Id = DevicePrefix + "Id";

                /// <summary></summary>
                public const string Language = DevicePrefix + "Language";

                /// <summary></summary>
                public const string Model = DevicePrefix + "Model";

                /// <summary></summary>
                public const string NetworkType = DevicePrefix + "NetworkType";

                /// <summary></summary>
                public const string OemName = DevicePrefix + "OemName";

                /// <summary></summary>
                public const string OperatingSystem = DevicePrefix + "OperatingSystem";

                /// <summary></summary>
                public const string ScreenResolution = DevicePrefix + "ScreenResolution";

                /// <summary></summary>
                public const string Type = DevicePrefix + "Type";
            }

            /// <summary></summary>
            public static class Location
            {
                private const string LocationPrefix = TelemetryContextPrefix + "Location.";

                /// <summary></summary>
                public const string Ip = LocationPrefix + "Ip";
            }

            /// <summary></summary>
            public static class Operation
            {
                private const string OperationPrefix = TelemetryContextPrefix + "Operation.";

                /// <summary></summary>
                public const string CorrelationVector = OperationPrefix + "CorrelationVector";

                /// <summary></summary>
                public const string Id = OperationPrefix + "Id";

                /// <summary></summary>
                public const string Name = OperationPrefix + "Name";

                /// <summary></summary>
                public const string ParentId = OperationPrefix + "ParentId";

                /// <summary></summary>
                public const string SyntheticSource = OperationPrefix + "SyntheticSource";
            }

            /// <summary></summary>
            public static class Session
            {
                private const string SessionPrefix = TelemetryContextPrefix + "Session.";


                /// <summary></summary>
                public const string Id = SessionPrefix + "Id";

                /// <summary></summary>
                public const string IsFirst = SessionPrefix + "IsFirst";
            }

            /// <summary></summary>
            public static class User
            {
                private const string UserPrefix = TelemetryContextPrefix + "User.";

                /// <summary></summary>
                public const string AccountId = UserPrefix + "AccountId";

                /// <summary></summary>
                public const string AuthenticatedUserId = UserPrefix + "AuthenticatedUserId";

                /// <summary></summary>
                public const string Id = UserPrefix + "Id";

                /// <summary></summary>
                public const string UserAgent = UserPrefix + "UserAgent";
            }
#pragma warning restore SA1202 // Elements must be ordered by access
        }
    }
}
