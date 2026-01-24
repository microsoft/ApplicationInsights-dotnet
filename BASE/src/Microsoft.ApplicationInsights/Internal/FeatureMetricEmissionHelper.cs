// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.ApplicationInsights.Internal
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text.Json;
    using Azure.Monitor.OpenTelemetry.Exporter.Internals.Diagnostics;
    using OpenTelemetry;

    internal sealed class FeatureMetricEmissionHelper : IDisposable
    {
        private static readonly ConcurrentDictionary<string, FeatureMetricEmissionHelper> HelperRegistry = new ();

        private readonly string resourceProvider;
        private readonly string ciKey;
        private readonly string version;
        private readonly Meter featureMeter = new (StatsbeatConstants.FeatureStatsbeatMeterName, "1.0");

        private string os;

        private StatsbeatFeatures observedFeatures = StatsbeatFeatures.None;

        private FeatureMetricEmissionHelper(string ciKey, string version)
        {
            this.os = GetOs();
            this.resourceProvider = this.GetResourceProvider();
            this.ciKey = ciKey;
            this.version = version;

            this.featureMeter.CreateObservableGauge(StatsbeatConstants.FeatureStatsbeatMetricName, () => this.GetFeatureStatsbeat());
        }

        public void Dispose()
        {
            if (this.featureMeter != null)
            {
                this.featureMeter.Dispose();
            }
        }

        internal static FeatureMetricEmissionHelper GetOrCreate(string ciKey, string version)
        {
            string key = $"{ciKey};{version}";

            return HelperRegistry.GetOrAdd(key, _ => 
            {
                return new FeatureMetricEmissionHelper(ciKey, version);
            });
        }

        internal void MarkFeatureInUse(StatsbeatFeatures features)
        {
            lock(this)
            {
                this.observedFeatures |= features;
            }
        }

        internal Measurement<int> GetFeatureStatsbeat()
        {
            if (this.observedFeatures == 0)
            {
                // If no features have been observed, then skip sending the feature measurement
                return new Measurement<int>();
            }

            try
            {
                return
                    new Measurement<int>(1,
                        new ("rp", this.resourceProvider),
                        new ("attach", "Manual"),
                        new ("cikey", this.ciKey),
                        new ("feature", (ulong)this.observedFeatures),
                        new ("type", 0), // 0 = feature, 1 = instrumentation scopes
                        new ("os", this.os),
                        new ("language", "dotnet"),
                        new ("product", "appinsights"),
                        new ("version", this.version));
            }
            catch (Exception)
            {
                // feature SDK stats isn't critical, so just skip it
                return new Measurement<int>();
            }
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
                    this.os = (osType as string).ToLower(CultureInfo.InvariantCulture);
                }
                else
                {
                    // this code reproduces a logic error in the exporter where if the osType is not available, 
                    // we overwrite a good platform-observed OS. This is maintained to ensure a match with the exporter's data.
                    this.os = "unknown";
                }

                return "vm";
            }

            return "unknown";
        }
    }
}
