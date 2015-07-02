// <copyright file="ComponentContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : ComponentContext.cs
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

    /// <summary>
    /// Encapsulates information describing an Application Insights component.
    /// </summary>
    /// <remarks>
    /// This class matches the "Application" schema concept. We are intentionally calling it "Component" for consistency 
    /// with terminology used by our portal and services and to encourage standardization of terminology within our 
    /// organization. Once a consensus is reached, we will change type and property names to match.
    /// </remarks>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class ComponentContextData
    {
        private readonly IDictionary<string, string> tags;

        internal ComponentContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Version
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.ApplicationVersion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.ApplicationVersion, value); }
        }

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string Build
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.ApplicationBuild); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.ApplicationBuild, value); }
        }

        internal void SetDefaults(ComponentContextData source)
        {
            this.tags.InitializeTagValue(ContextTagKeys.Keys.ApplicationVersion, source.Version);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.ApplicationBuild, source.Build);
        }
    }
}
