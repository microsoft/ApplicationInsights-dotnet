// <copyright file="SessionContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : SessionContext.cs
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
    /// Encapsulates information about a user session.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class SessionContextData
    {
        private readonly IDictionary<string, string> tags;

        internal SessionContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the application-defined session ID.
        /// </summary>
        public string Id
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.SessionId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.SessionId, value); }
        }

        /// <summary>
        /// Gets or sets the IsFirst Session for the user.
        /// </summary>
        public bool? IsFirst 
        {
            get { return this.tags.GetTagBoolValueOrNull(ContextTagKeys.Keys.SessionIsFirst); }
            set { this.tags.SetTagValueOrRemove<bool?>(ContextTagKeys.Keys.SessionIsFirst, value); }
        }

        /// <summary>
        /// Gets or sets the IsNewSession Session.
        /// </summary>
        public bool? IsNewSession 
        {
            get { return this.tags.GetTagBoolValueOrNull(ContextTagKeys.Keys.SessionIsNew); }
            set { this.tags.SetTagValueOrRemove<bool?>(ContextTagKeys.Keys.SessionIsNew, value); }
        }

        internal void SetDefaults(SessionContextData source)
        {
            this.tags.InitializeTagValue(ContextTagKeys.Keys.SessionId, source.Id);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.SessionIsFirst, source.IsFirst);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.SessionIsNew, source.IsNewSession);
        }
    }
}
