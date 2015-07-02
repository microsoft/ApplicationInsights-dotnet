// <copyright file="UserContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : UserContext.cs
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
    /// Encapsulates information about a user using an application.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class UserContextData
    {
        private readonly IDictionary<string, string> tags;

        internal UserContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the ID of user accessing the application.
        /// </summary>
        /// <remarks>
        /// Unique user ID is automatically generated in default Application Insights configuration. 
        /// </remarks>
        public string Id 
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserId, value); }
        }

        /// <summary>
        /// Gets or sets the ID of an application-defined account associated with the user.
        /// </summary>
        public string AccountId
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserAccountId); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserAccountId, value); }
        }

        /// <summary>
        /// Gets or sets the UserAgent of an application-defined account associated with the user.
        /// </summary>
        public string UserAgent
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserAgent); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserAgent, value); }
        }

        /// <summary>
        /// Gets or sets the StoreRegion of an application-defined account associated with the user.
        /// </summary>
        public string StoreRegion
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.UserStoreRegion); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.UserStoreRegion, value); }
        }

        /// <summary>
        /// Gets or sets the date when the user accessed the application for the first time.
        /// </summary>
        /// <remarks>
        /// Acquisition date is automatically supplied in default Application Insights configuration.
        /// </remarks>
        public DateTimeOffset? AcquisitionDate 
        {
            get { return this.tags.GetTagDateTimeOffsetValueOrNull(ContextTagKeys.Keys.UserAccountAcquisitionDate); }
            set { this.tags.SetDateTimeOffsetValueOrRemove(ContextTagKeys.Keys.UserAccountAcquisitionDate, value); }
        }
        
        /// <summary>
        /// Sets values on the current context based on the default context passed in.
        /// </summary>
        internal void SetDefaults(UserContextData source)
        {
            this.tags.InitializeTagValue(ContextTagKeys.Keys.UserId, source.Id);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.UserAgent, source.UserAgent);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.UserAccountId, source.AccountId);
            this.tags.InitializeTagDateTimeOffsetValue(ContextTagKeys.Keys.UserAccountAcquisitionDate, source.AcquisitionDate);
            this.tags.InitializeTagValue(ContextTagKeys.Keys.UserStoreRegion, source.StoreRegion);
        }
    }
}
