// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInsights.Internal
{
    using Azure.Monitor.OpenTelemetry.Exporter.Internals.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Net.Http;
    using System.Text.Json;
    using System.Runtime.InteropServices;
    using OpenTelemetry;

    internal sealed class FeatureMetricEmissionHelper : IDisposable
    {
        private static readonly Dictionary<string, FeatureMetricEmissionHelper> helperRegistry = new ();

        private readonly string resourceProvider;
        private readonly string ciKey;
        private readonly string version;
        private readonly Meter featureMeter = new (StatsbeatConstants.FeatureStatsbeatMeterName, "1.0");

        private string os;

        private StatsbeatFeatures observedFeatures = StatsbeatFeatures.None;

        private FeatureMetricEmissionHelper(string ciKey, string version)
        {
            this.os = GetOs();
            this.resourceProvider = GetResourceProvider();
            this.ciKey = ciKey;
            this.version = version;

            this.featureMeter.CreateObservableGauge(StatsbeatConstants.FeatureStatsbeatMetricName, () => GetFeatureStatsbeat());
        }

        public void Dispose()
        {
            if (featureMeter != null)
            {
                featureMeter.Dispose();
            }
        }

        internal static FeatureMetricEmissionHelper GetOrCreate(string ciKey, string version)
        {
            string key = $"{ciKey};{version}";

            if (FeatureMetricEmissionHelper.helperRegistry.TryGetValue(key, out FeatureMetricEmissionHelper helper))
            {
                return helper;
            }

            helper = new FeatureMetricEmissionHelper(ciKey, version);
            helperRegistry.Add(key, helper);
            return helper;
        }

        private static string GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "windows";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx";
            }

            return "unknown";
        }

        private static Dictionary<string, object> GetVmMetadata()
        {
            try
            {
                // Prevent internal HTTP operations from being instrumented.
                using (var scope = SuppressInstrumentationScope.Begin())
                {
                    using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(2) })
                    {
                        httpClient.DefaultRequestHeaders.Add("Metadata", "True");
                        var responseString = httpClient.GetStringAsync(StatsbeatConstants.AMSUrl);
                        return JsonSerializer.Deserialize<Dictionary<string, object>>(responseString.Result);
                    }
                }
            }
            catch (Exception)
            {
                // If the OS isn't available for any reason, return empty ("unknown")
                return null;
            }
        }

        internal void MarkFeatureInUse(StatsbeatFeatures features)
        {
            observedFeatures |= features;
        }

        internal Measurement<int> GetFeatureStatsbeat()
        {
            if (observedFeatures == 0)
            {
                // If no features have been observed, then skip sending the feature measurement
                return new Measurement<int>();
            }

            try
            {
                return
                    new Measurement<int>(1,
                        new ("rp", resourceProvider),
                        new ("attach", "Manual"),
                        new ("cikey", ciKey),
                        new ("feature", (ulong)observedFeatures),
                        new ("type", 0), // 0 = feature, 1 = instrumentation scopes
                        new ("os", os),
                        new ("language", "dotnet"),
                        new ("product", "appinsights"),
                        new ("version", version));
            }
            catch (Exception)
            {
                // feature SDK stats isn't critical, so just skip it
                return new Measurement<int>();
            }
        }

        /// <summary>
        /// Inspect environment variables and VM instance metadata service (IMDS) for the azure resource provider name (eg., "aks").
        ///
        /// As a side effect, also updates the OS field if that info is available from IMDS.
        /// </summary>
        /// <returns>the resource provider.</returns>
        private string GetResourceProvider()
        {
            var functionsWorkerRuntime = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME");
            if (functionsWorkerRuntime != null)
            {
                return "functions";
            }

            var appSvcWebsiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");
            if (appSvcWebsiteName != null)
            {
                return "appsvc";
            }

            var aksArmNamespaceId = Environment.GetEnvironmentVariable("AKS_ARM_NAMESPACE_ID");
            if (aksArmNamespaceId != null)
            {
                return "aks";
            }

            var vmMetadata = GetVmMetadata();

            if (vmMetadata != null)
            {
                if (vmMetadata.TryGetValue("osType", out var osType) && osType is string)
                {
                    // osType takes precedence over the platform-observed OS.
                    os = (osType as string).ToLower(CultureInfo.InvariantCulture);
                }
                else
                {
                    // this code reproduces a logic error in the exporter where if the osType is not available, 
                    // we overwrite a good platform-observed OS. This is maintained to ensure a match with the exporter's data.
                    os = "unknown";
                }

                return "vm";
            }

            return "unknown";
        }
    }
}
