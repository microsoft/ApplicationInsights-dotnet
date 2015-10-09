// <copyright file="ContextTagKeys.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : ContextTagKeys.cs
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
    using System.Threading;

    /// <summary>
    /// Holds the static singleton instance of ContextTagKeys.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    partial class ContextTagKeys
    {
        private static ContextTagKeys keys;

        internal static ContextTagKeys Keys
        {
            get { return LazyInitializer.EnsureInitialized(ref keys); }
        }
    }
}