// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInsights.Internal
{
    using System;

    /// <summary>
    /// This enum models the Application Insights feature mapping.
    /// </summary>
    [Flags]
    internal enum StatsbeatFeatures : ulong
    {
        /// <summary>
        /// Represents no features.
        /// </summary>
        None = 0,

        /// <summary>
        /// This feature measures whether the TrackEvent API in Application Insights is used.
        /// </summary>
        TrackEvent = 1 << 0,

        /// <summary>
        /// This feature measures whether the TrackAvailability API in Application Insights is used.
        /// </summary>
        TrackAvailability = 1 << 1,

        /// <summary>
        /// This feature measures whether the TrackTrace API in Application Insights is used.
        /// </summary>
        TrackTrace = 1 << 2,

        /// <summary>
        /// This feature measures whether the TrackMetric API in Application Insights is used.
        /// </summary>
        TrackMetric = 1 << 3,

        /// <summary>
        /// This feature measures whether the TrackException API in Application Insights is used.
        /// </summary>
        TrackException = 1 << 4,

        /// <summary>
        /// This feature measures whether the TrackDependency API in Application Insights is used.
        /// </summary>
        TrackDependency = 1 << 5,

        /// <summary>
        /// This feature measures whether the TrackRequest API in Application Insights is used.
        /// </summary>
        TrackRequest = 1 << 6,

        /// <summary>
        /// This feature measures whether the StartOperation API in Application Insights is used.
        /// </summary>
        StartOperation = 1 << 7,
    }
}