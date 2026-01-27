// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// This is copied from https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/monitor/Azure.Monitor.OpenTelemetry.Exporter/src/Internals/Statsbeat/StatsbeatConstants.cs

namespace Microsoft.ApplicationInsights.Internal
{
    using System;
    using System.Collections.Generic;

    internal static class StatsbeatConstants
    {
        /// <summary>
        /// <see href="https://learn.microsoft.com/azure/virtual-machines/instance-metadata-service"/>.
        /// </summary>
        internal const string AMSUrl = "http://169.254.169.254/metadata/instance/compute?api-version=2017-08-01&format=json";

        /// <summary>
        /// 24 hrs == 86400000 milliseconds.
        /// </summary>
        internal const int AttachStatsbeatInterval = 86400000;
        internal const string AttachStatsbeatMeterName = "AttachStatsbeatMeter";
        internal const string AttachStatsbeatMetricName = "Attach";

        /// <summary>
        /// 24 hrs == 86400000 milliseconds.
        /// </summary>
        internal const int FeatureStatsbeatInterval = 86400000;
        internal const string FeatureStatsbeatMeterName = "FeatureStatsbeatMeter";
        internal const string FeatureStatsbeatMetricName = "Feature";
    }
}
