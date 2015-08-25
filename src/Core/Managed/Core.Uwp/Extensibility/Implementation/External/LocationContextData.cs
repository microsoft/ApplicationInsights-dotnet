// <copyright file="LocationContextData.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

//------------------------------------------------------------------------------
//
//     This code was updated from a master copy in the DataCollectionSchemas repo.
// 
//     Repo     : DataCollectionSchemas
//     File     : LocationContext.cs
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
    using System.Text;

    /// <summary>
    /// Encapsulates telemetry location information.
    /// </summary>
#if DATAPLATFORM
    public
#else
    internal
#endif
    sealed class LocationContextData
    {
        private readonly IDictionary<string, string> tags;

        internal LocationContextData(IDictionary<string, string> tags)
        {
            this.tags = tags;
        }

        /// <summary>
        /// Gets or sets the location IP.
        /// </summary>
        public string Ip
        {
            get { return this.tags.GetTagValueOrNull(ContextTagKeys.Keys.LocationIp); }
            set { this.tags.SetStringValueOrRemove(ContextTagKeys.Keys.LocationIp, value); }
        }

#if DATAPLATFORM
        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string City
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationCity); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationCity, value); }
        }

        /// <summary>
        /// Gets or sets the continent.
        /// </summary>
        public string Continent
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationContinent); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationContinent, value); }
        }

        /// <summary>
        /// Gets or sets the country.
        /// </summary>
        public string Country
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationCountry); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationCountry, value); }
        }

        /// <summary>
        /// Gets or sets the latitude.
        /// </summary>
        public string Latitude
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationLatitude); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationLatitude, value); }
        }

        /// <summary>
        /// Gets or sets the longitude.
        /// </summary>
        public string Longitude
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationLongitude); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationLongitude, value); }
        }

        /// <summary>
        /// Gets or sets the province.
        /// </summary>
        public string Province
        {
            get { return this.tags.GetTagValueOrNull(IngestTagKeys.Keys.LocationProvince); }
            set { this.tags.SetStringValueOrRemove(IngestTagKeys.Keys.LocationProvince, value); }
        }
#endif
    }
}
