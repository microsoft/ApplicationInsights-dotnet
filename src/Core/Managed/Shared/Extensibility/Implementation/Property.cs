// <copyright file="Property.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// A helper class for implementing properties of telemetry and context classes.
    /// </summary>
    internal static class Property
    {
        public const int MaxDictionaryNameLength = 150;
        public const int MaxDependencyTypeLength = 1024;
        public const int MaxValueLength = 8 * 1024;
        public const int MaxResultCodeLength = 1024;
        public const int MaxEventNameLength = 512;
        public const int MaxNameLength = 1024;
        public const int MaxMessageLength = 32768;
        public const int MaxUrlLength = 2048;
        public const int MaxDataLength = 8 * 1024;
        public const int MaxTestNameLength = 1024;
        public const int MaxRunLocationLength = 2024;
        public const int MaxAvailabilityMessageLength = 8192;
        public const int MaxApplicationVersionLength = 1024;
        public const int MaxDeviceIdLength = 1024;
        public const int MaxDeviceModelLength = 256;
        public const int MaxDeviceOemNameLength = 256;
        public const int MaxDeviceOperatingSystemLength = 256;
        public const int MaxDeviceTypeLength = 64;
        public const int MaxLocationIpLength = 45;
        public const int MaxOperationIdLength = 128;
        public const int MaxOperationNameLength = 1024;
        public const int MaxOperationParentIdLength = 128;
        public const int MaxOperationSyntheticSourceLength = 1024;
        public const int MaxOperationCorrelationVectorLength = 64;
        public const int MaxSessionIdLength = 64;
        public const int MaxSessionIsFirstLength = 5;
        public const int MaxUserIdLength = 128;
        public const int MaxUserAccountIdLength = 1024;
        public const int MaxUserAuthenticatedIdLength = 1024;
        public const int MaxUserAgentLength = 2048;
        public const int MaxCloudRoleNameLength = 256;
        public const int MaxCloudRoleInstanceLength = 256;
        public const int MaxInternalSdkVersionLength = 64;
        public const int MaxInternalAgentVersionLength = 64;
        public const int MaxInternalNodeNameLength = 256;

        public static void Set<T>(ref T property, T value) where T : class
        {
            if (value == default(T))
            {
                throw new ArgumentNullException("value");
            }

            property = value;
        }

        public static void Initialize<T>(ref T? property, T? value) where T : struct
        {
            if (!property.HasValue)
            {
                property = value;
            }
        }

        public static void Initialize(ref string property, string value)
        {
            if (string.IsNullOrEmpty(property))
            {
                property = value;
            }
        }

        public static string SanitizeEventName(this string name)
        {
            return TrimAndTruncate(name, Property.MaxEventNameLength);
        }

        public static string SanitizeName(this string name)
        {
            return TrimAndTruncate(name, Property.MaxNameLength);
        }

        public static string SanitizeDependencyType(this string value)
        {
            return TrimAndTruncate(value, Property.MaxDependencyTypeLength);
        }

        public static string SanitizeResultCode(this string value)
        {
            return TrimAndTruncate(value, Property.MaxResultCodeLength);
        }

        public static string SanitizeValue(this string value)
        {
            return TrimAndTruncate(value, Property.MaxValueLength);
        }

        public static string SanitizeMessage(this string message)
        {
            return TrimAndTruncate(message, Property.MaxMessageLength);
        }

        public static string SanitizeData(this string message)
        {
            return TrimAndTruncate(message, Property.MaxDataLength);
        }

        public static Uri SanitizeUri(this Uri uri)
        {
            if (uri != null)
            {
                string url = uri.ToString();

                if (url.Length > MaxUrlLength)
                {
                    url = url.Substring(0, MaxUrlLength);

                    // in case that the truncated string is invalid 
                    // URI we will not do nothing and let the Endpoint to drop the property
                    Uri temp;
                    if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out temp) == true)
                    {
                        uri = temp;
                    }
                }
            }

            return uri;
        }

        public static string SanitizeTestName(this string value)
        {
            return TrimAndTruncate(value, Property.MaxTestNameLength);
        }

        public static string SanitizeRunLocation(this string value)
        {
            return TrimAndTruncate(value, Property.MaxRunLocationLength);
        }

        public static string SanitizeAvailabilityMessage(this string value)
        {
            return TrimAndTruncate(value, Property.MaxAvailabilityMessageLength);
        }

        public static void SanitizeTelemetryContext(this TelemetryContext telemetryContext)
        {
            SanitizeComponentContext(telemetryContext.Component);            
            SanitizeDeviceContext(telemetryContext.Device);
            SanitizeLocationContext(telemetryContext.Location);                                    
            SanitizeOperationContext(telemetryContext.Operation);            
            SanitizeSessionContext(telemetryContext.Session); 
            SanitizeUserContext(telemetryContext.User);
            SanitizeCloudContext(telemetryContext.Cloud);
            SanitizeInternalContext(telemetryContext.Internal);                        
        }        

        public static void SanitizeProperties(this IDictionary<string, string> dictionary)
        {
            if (dictionary != null)
            {
                var sanitizedEntries = new Dictionary<string, KeyValuePair<string, string>>(dictionary.Count);

                foreach (KeyValuePair<string, string> entry in dictionary)
                {
                    string sanitizedKey = SanitizeKey(entry.Key);
                    string sanitizedValue = SanitizeValue(entry.Value);

                    if (string.IsNullOrEmpty(sanitizedValue) || (string.CompareOrdinal(sanitizedKey, entry.Key) != 0) || (string.CompareOrdinal(sanitizedValue, entry.Value) != 0))
                    {
                        sanitizedEntries.Add(entry.Key, new KeyValuePair<string, string>(sanitizedKey, sanitizedValue));
                    }
                }

                foreach (KeyValuePair<string, KeyValuePair<string, string>> entry in sanitizedEntries)
                {
                    dictionary.Remove(entry.Key);

                    if (!string.IsNullOrEmpty(entry.Value.Value))
                    {
                        string uniqueKey = MakeKeyUnique(entry.Value.Key, dictionary);
                        dictionary.Add(uniqueKey, entry.Value.Value);
                    }
                }
            }
        }

        public static void SanitizeMeasurements(this IDictionary<string, double> dictionary)
        {
            if (dictionary != null)
            {
                var sanitizedEntries = new Dictionary<string, KeyValuePair<string, double>>(dictionary.Count);

                foreach (KeyValuePair<string, double> entry in dictionary)
                {
                    string sanitizedKey = SanitizeKey(entry.Key);

                    bool valueChanged;
                    double sanitizedValue = Utils.SanitizeNanAndInfinity(entry.Value, out valueChanged);

                    if ((string.CompareOrdinal(sanitizedKey, entry.Key) != 0) || valueChanged)
                    {
                        sanitizedEntries.Add(entry.Key, new KeyValuePair<string, double>(sanitizedKey, sanitizedValue));
                    }
                }

                foreach (KeyValuePair<string, KeyValuePair<string, double>> entry in sanitizedEntries)
                {
                    dictionary.Remove(entry.Key);
                    string uniqueKey = MakeKeyUnique(entry.Value.Key, dictionary);
                    dictionary.Add(uniqueKey, entry.Value.Value);
                }
            }
        }

        private static string TrimAndTruncate(string value, int maxLength)
        {
            if (value != null)
            {
                value = value.Trim();
                value = Truncate(value, maxLength);
            }

            return value;
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        private static string SanitizeKey(string key)
        {
            string sanitizedKey = TrimAndTruncate(key, Property.MaxDictionaryNameLength);
            return MakeKeyNonEmpty(sanitizedKey);
        }

        private static string MakeKeyNonEmpty(string key)
        {
            return string.IsNullOrEmpty(key) ? "required" : key;
        }

        private static string MakeKeyUnique<TValue>(string key, IDictionary<string, TValue> dictionary)
        {
            if (dictionary.ContainsKey(key))
            {
                const int UniqueNumberLength = 3;
                string truncatedKey = Truncate(key, MaxDictionaryNameLength - UniqueNumberLength);
                int candidate = 1;
                do
                {
                    key = truncatedKey + candidate;
                    ++candidate;
                }
                while (dictionary.ContainsKey(key));
            }

            return key;
        }

        private static void SanitizeComponentContext(ComponentContext componentContext)
        {
            componentContext.Version = TrimAndTruncate(componentContext.Version, Property.MaxApplicationVersionLength);
        }

        private static void SanitizeDeviceContext(DeviceContext deviceContext)
        {
            deviceContext.Id = TrimAndTruncate(deviceContext.Id, Property.MaxDeviceIdLength);
            deviceContext.Model = TrimAndTruncate(deviceContext.Model, Property.MaxDeviceModelLength);
            deviceContext.OemName = TrimAndTruncate(deviceContext.OemName, Property.MaxDeviceOemNameLength);
            deviceContext.OperatingSystem = TrimAndTruncate(deviceContext.OperatingSystem, Property.MaxDeviceOperatingSystemLength);
            deviceContext.Type = TrimAndTruncate(deviceContext.Type, Property.MaxDeviceTypeLength);
        }

        private static void SanitizeLocationContext(LocationContext locationContext)
        {
            locationContext.Ip = TrimAndTruncate(locationContext.Ip, Property.MaxLocationIpLength);
        }

        private static void SanitizeOperationContext(OperationContext operationContext)
        {
            operationContext.Id = TrimAndTruncate(operationContext.Id, Property.MaxOperationIdLength);
            operationContext.Name = TrimAndTruncate(operationContext.Name, Property.MaxOperationNameLength);
            operationContext.ParentId = TrimAndTruncate(operationContext.ParentId, Property.MaxOperationParentIdLength);
            operationContext.SyntheticSource = TrimAndTruncate(operationContext.SyntheticSource, Property.MaxOperationSyntheticSourceLength);
            operationContext.CorrelationVector = TrimAndTruncate(operationContext.CorrelationVector, Property.MaxOperationCorrelationVectorLength);
        }

        private static void SanitizeSessionContext(SessionContext sessionContext)
        {
            sessionContext.Id = TrimAndTruncate(sessionContext.Id, Property.MaxSessionIdLength);
        }

        private static void SanitizeUserContext(UserContext userContext)
        {
            userContext.Id = TrimAndTruncate(userContext.Id, Property.MaxUserIdLength);
            userContext.AccountId = TrimAndTruncate(userContext.AccountId, Property.MaxUserAccountIdLength);
            userContext.UserAgent = TrimAndTruncate(userContext.UserAgent, Property.MaxUserAgentLength);
            userContext.AuthenticatedUserId = TrimAndTruncate(userContext.AuthenticatedUserId, Property.MaxUserAuthenticatedIdLength);
        }

        private static void SanitizeCloudContext(CloudContext cloudContext)
        {
            cloudContext.RoleName = TrimAndTruncate(cloudContext.RoleName, Property.MaxCloudRoleNameLength);
            cloudContext.RoleInstance = TrimAndTruncate(cloudContext.RoleInstance, Property.MaxCloudRoleInstanceLength);
        }

        private static void SanitizeInternalContext(InternalContext internalContext)
        {
            internalContext.SdkVersion = TrimAndTruncate(internalContext.SdkVersion, Property.MaxInternalSdkVersionLength);
            internalContext.AgentVersion = TrimAndTruncate(internalContext.AgentVersion, Property.MaxInternalAgentVersionLength);
            internalContext.NodeName = TrimAndTruncate(internalContext.NodeName, Property.MaxInternalNodeNameLength);
        }
    }
}
