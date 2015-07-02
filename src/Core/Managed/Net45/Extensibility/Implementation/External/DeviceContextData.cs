// <copyright file="DeviceContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : DeviceContext.cs
//
//     Changes to this file may cause incorrect behavior and will be lost when
//     the code is updated.
//
//------------------------------------------------------------------------------

#if DATAPLATFORM
namespace Microsoft.Developer.Analytics.DataCollection.Model.v2
#else
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
#endif
{
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Encapsulates information about a device where an application is running.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class DeviceContextData
    {
        private readonly IDictionary<string, string> tags;

        internal DeviceContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }
        
        /// <summary>
        /// Gets or sets the type for the current device.
        /// </summary>
        public string Type
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceType); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceType, value); }
        }

        /// <summary>
        /// Gets or sets a device unique ID.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceId, value); }
        }

        /// <summary>
        /// Gets or sets the operating system name.
        /// </summary>
        public string OperatingSystem
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceOS); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceOS, value); }
        }

        /// <summary>
        /// Gets or sets the device OEM for the current device.
        /// </summary>
        public string OemName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceOEMName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceOEMName, value); }
        }

        /// <summary>
        /// Gets or sets the device model for the current device.
        /// </summary>
        public string Model
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceModel); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceModel, value); }
        }

        /// <summary>
        /// Gets or sets the <a href="http://www.iana.org/assignments/ianaiftype-mib/ianaiftype-mib">IANA interface type</a> 
        /// for the internet connected network adapter.
        /// </summary>
        public int? NetworkType
        {
            get { return this.tags.GetTagIntValueOrNull(ContextTagKeys.Keys.DeviceNetwork); }
            set { this.tags.SetTagValueOrRemove(ContextTagKeys.Keys.DeviceNetwork, value); }
        }

        /// <summary>
        /// Gets or sets the current application screen resolution.
        /// </summary>
        public string ScreenResolution
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceScreenResolution); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceScreenResolution, value); }
        }

        /// <summary>
        /// Gets or sets the current display language of the operating system.
        /// </summary>
        public string Language
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceLanguage); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceLanguage, value); }
        }

        /// <summary>
        /// Gets or sets the role name.
        /// </summary>
        public string RoleName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceRoleName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceRoleName, value); }
        }

        /// <summary>
        /// Gets or sets the role instance.
        /// </summary>
        public string RoleInstance
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceRoleInstance); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceRoleInstance, value); }
        }

        /// <summary>
        /// Gets or sets the device IP address.
        /// </summary>
        public string Ip
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceIp); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceIp, value); }
        }

        /// <summary>
        /// Gets or sets the device VM name.
        /// </summary>
        public string MachineName
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.DeviceMachineName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.DeviceMachineName, value); }
        }

        internal void SetDefaults(DeviceContextData source)
        {
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceType, source.Type);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceId, source.Id);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceOS, source.OperatingSystem);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceOEMName, source.OemName);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceModel, source.Model);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceNetwork, source.NetworkType);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceScreenResolution, source.ScreenResolution);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceLanguage, source.Language);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceIp, source.Ip);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.DeviceMachineName, source.MachineName);
        }
    }
}
