
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : ContextTagKeys_types.cs
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// <auto-generated />
//------------------------------------------------------------------------------


// suppress "Missing XML comment for publicly visible type or member"
#pragma warning disable 1591


#region ReSharper warnings
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective
#endregion

namespace AI
{
    using System.Collections.Generic;

    // [global::Bond.Attribute("ContextContract", "Emit")]
    // [global::Bond.Attribute("PseudoType", "JSMap")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class ContextTagKeys
    {
        // [global::Bond.Attribute("Description", "Application version. Information in the application context fields is always about the application that is sending the telemetry.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(10)]
        public string ApplicationVersion { get; set; }

        // [global::Bond.Attribute("Description", "Unique client device id. Computer name in most cases.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(100)]
        public string DeviceId { get; set; }

        // [global::Bond.Attribute("Description", "Device locale using <language>-<REGION> pattern, following RFC 5646. Example 'en-US'.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(115)]
        public string DeviceLocale { get; set; }

        // [global::Bond.Attribute("Description", "Model of the device the end user of the application is using. Used for client scenarios. If this field is empty then it is derived from the user agent.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(120)]
        public string DeviceModel { get; set; }

        // [global::Bond.Attribute("Description", "Client device OEM name taken from the browser.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(130)]
        public string DeviceOEMName { get; set; }

        // [global::Bond.Attribute("Description", "Operating system name and version of the device the end user of the application is using. If this field is empty then it is derived from the user agent. Example 'Windows 10 Pro 10.0.10586.0'")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(140)]
        public string DeviceOSVersion { get; set; }

        // [global::Bond.Attribute("Description", "The type of the device the end user of the application is using. Used primarily to distinguish JavaScript telemetry from server side telemetry. Examples: 'PC', 'Phone', 'Browser'. 'PC' is the default value.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(160)]
        public string DeviceType { get; set; }

        // [global::Bond.Attribute("Description", "The IP address of the client device. IPv4 and IPv6 are supported. Information in the location context fields is always about the end user. When telemetry is sent from a service, the location context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "46")]
        // [global::Bond.Id(200)]
        public string LocationIp { get; set; }

        // [global::Bond.Attribute("Description", "The country of the client device. If any of Country, Province, or City is specified, those values will be preferred over geolocation of the IP address field. Information in the location context fields is always about the end user. When telemetry is sent from a service, the location context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(201)]
        public string LocationCountry { get; set; }

        // [global::Bond.Attribute("Description", "The province/state of the client device. If any of Country, Province, or City is specified, those values will be preferred over geolocation of the IP address field. Information in the location context fields is always about the end user. When telemetry is sent from a service, the location context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(202)]
        public string LocationProvince { get; set; }

        // [global::Bond.Attribute("Description", "The city of the client device. If any of Country, Province, or City is specified, those values will be preferred over geolocation of the IP address field. Information in the location context fields is always about the end user. When telemetry is sent from a service, the location context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(203)]
        public string LocationCity { get; set; }

        // [global::Bond.Attribute("Description", "A unique identifier for the operation instance. The operation.id is created by either a request or a page view. All other telemetry sets this to the value for the containing request or page view. Operation.id is used for finding all the telemetry items for a specific operation instance.")]
        // [global::Bond.Attribute("MaxStringLength", "128")]
        // [global::Bond.Id(300)]
        public string OperationId { get; set; }

        // [global::Bond.Attribute("Description", "The name (group) of the operation. The operation.name is created by either a request or a page view. All other telemetry items set this to the value for the containing request or page view. Operation.name is used for finding all the telemetry items for a group of operations (i.e. 'GET Home/Index').")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(305)]
        public string OperationName { get; set; }

        // [global::Bond.Attribute("Description", "The unique identifier of the telemetry item's immediate parent.")]
        // [global::Bond.Attribute("MaxStringLength", "128")]
        // [global::Bond.Id(310)]
        public string OperationParentId { get; set; }

        // [global::Bond.Attribute("Description", "Name of synthetic source. Some telemetry from the application may represent a synthetic traffic. It may be web crawler indexing the web site, site availability tests or traces from diagnostic libraries like Application Insights SDK itself.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(320)]
        public string OperationSyntheticSource { get; set; }

        // [global::Bond.Attribute("Description", "The correlation vector is a light weight vector clock which can be used to identify and order related events across clients and services.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(330)]
        public string OperationCorrelationVector { get; set; }

        // [global::Bond.Attribute("Description", "Session ID - the instance of the user's interaction with the app. Information in the session context fields is always about the end user. When telemetry is sent from a service, the session context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(400)]
        public string SessionId { get; set; }

        // [global::Bond.Attribute("Description", "Boolean value indicating whether the session identified by ai.session.id is first for the user or not.")]
        // [global::Bond.Attribute("MaxStringLength", "5")]
        // [global::Bond.Attribute("Question", "Should it be marked as JSType-bool for breeze?")]
        // [global::Bond.Id(405)]
        public string SessionIsFirst { get; set; }

        // [global::Bond.Attribute("Description", "In multi-tenant applications this is the account ID or name which the user is acting with. Examples may be subscription ID for Azure portal or blog name blogging platform.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(505)]
        public string UserAccountId { get; set; }

        // [global::Bond.Attribute("Description", "Anonymous user id. Represents the end user of the application. When telemetry is sent from a service, the user context is about the user that initiated the operation in the service.")]
        // [global::Bond.Attribute("MaxStringLength", "128")]
        // [global::Bond.Id(515)]
        public string UserId { get; set; }

        // [global::Bond.Attribute("Description", "Authenticated user id. The opposite of ai.user.id, this represents the user with a friendly name. Since it's PII information it is not collected by default by most SDKs.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(525)]
        public string UserAuthUserId { get; set; }

        // [global::Bond.Attribute("Description", "Name of the role the application is a part of. Maps directly to the role name in azure.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(705)]
        public string CloudRole { get; set; }

        // [global::Bond.Attribute("Description", "Name of the instance where the application is running. Computer name for on-premisis, instance name for Azure.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(715)]
        public string CloudRoleInstance { get; set; }

        // [global::Bond.Attribute("Description", "SDK version. See https://github.com/Microsoft/ApplicationInsights-Home/blob/master/SDK-AUTHORING.md#sdk-version-specification for information.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(1000)]
        public string InternalSdkVersion { get; set; }

        // [global::Bond.Attribute("Description", "Agent version. Used to indicate the version of StatusMonitor installed on the computer if it is used for data collection.")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(1001)]
        public string InternalAgentVersion { get; set; }

        // [global::Bond.Attribute("Description", "This is the node name used for billing purposes. Use it to override the standard detection of nodes.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(1002)]
        public string InternalNodeName { get; set; }

        public ContextTagKeys()
            : this("AI.ContextTagKeys", "ContextTagKeys")
        {}

        protected ContextTagKeys(string fullName, string name)
        {
            ApplicationVersion = "ai.application.ver";
            DeviceId = "ai.device.id";
            DeviceLocale = "ai.device.locale";
            DeviceModel = "ai.device.model";
            DeviceOEMName = "ai.device.oemName";
            DeviceOSVersion = "ai.device.osVersion";
            DeviceType = "ai.device.type";
            LocationIp = "ai.location.ip";
            LocationCountry = "ai.location.country";
            LocationProvince = "ai.location.province";
            LocationCity = "ai.location.city";
            OperationId = "ai.operation.id";
            OperationName = "ai.operation.name";
            OperationParentId = "ai.operation.parentId";
            OperationSyntheticSource = "ai.operation.syntheticSource";
            OperationCorrelationVector = "ai.operation.correlationVector";
            SessionId = "ai.session.id";
            SessionIsFirst = "ai.session.isFirst";
            UserAccountId = "ai.user.accountId";
            UserId = "ai.user.id";
            UserAuthUserId = "ai.user.authUserId";
            CloudRole = "ai.cloud.role";
            CloudRoleInstance = "ai.cloud.roleInstance";
            InternalSdkVersion = "ai.internal.sdkVersion";
            InternalAgentVersion = "ai.internal.agentVersion";
            InternalNodeName = "ai.internal.nodeName";
        }
    }
} // AI
