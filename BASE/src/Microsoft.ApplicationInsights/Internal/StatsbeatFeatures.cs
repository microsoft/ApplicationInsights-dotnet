// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.ApplicationInsights.Internal
{
    /// <summary>
    /// This enum models the Application Insights feature mapping.
    /// </summary>
    [Flags]
    enum StatsbeatFeatures : ulong
    {
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
    }
}