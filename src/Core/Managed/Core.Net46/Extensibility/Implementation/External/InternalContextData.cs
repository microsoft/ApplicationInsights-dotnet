// <copyright file="InternalContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : InternalContext.cs
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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Internal context type shared between SDK and DP.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed partial class InternalContextData
    {
        private readonly IDictionary<string, string> tags;

        internal InternalContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        public string SdkVersion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.InternalSdkVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.InternalSdkVersion, value); }
        }

        public string AgentVersion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.InternalAgentVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.InternalAgentVersion, value); }
        }
    }
}