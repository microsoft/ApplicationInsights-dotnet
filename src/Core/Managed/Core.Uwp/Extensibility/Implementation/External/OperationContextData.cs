// <copyright file="OperationContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : OperationContext.cs
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
    /// Encapsulates information about a user session.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class OperationContextData
    {
        private readonly IDictionary<string, string> tags;

        internal OperationContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application-defined operation ID.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationId, value); }
        }

        /// <summary>
        /// Gets or sets the application-defined operation NAME.
        /// </summary>
        public string Name
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationName); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationName, value); }
        }

        public string ParentId
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationParentId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationParentId, value); }
        }

        public string RootId
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationRootId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationRootId, value); }
        }

        public string SyntheticSource
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.OperationSyntheticSource); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.OperationSyntheticSource, value); }
        }

        public bool? IsSynthetic
        {
            get { return this.tags.GetTagBoolValueOrNull(ContextTagKeys.Keys.OperationIsSynthetic); }
            set { this.tags.SetTagValueOrRemove(ContextTagKeys.Keys.OperationIsSynthetic, value); }
        }

        internal void SetDefaults(OperationContextData source)
        {
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationId, source.Id);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationName, source.Name);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationParentId, source.ParentId);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationRootId, source.RootId);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationSyntheticSource, source.SyntheticSource);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.OperationIsSynthetic, source.IsSynthetic);
        }
    }
}
