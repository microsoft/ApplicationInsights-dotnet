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
    using System.Threading;
    using Azure.Monitor.OpenTelemetry.Exporter.Internals.Diagnostics;
    using OpenTelemetry;

    internal sealed class FeatureMetricEmissionHelper : IDisposable
    {
        private static readonly ConcurrentDictionary<string, FeatureMetricEmissionHelper> HelperRegistry = new ();

        // Resolved on first use (metric callback) so the constructor never blocks on IMDS.
        private readonly Lazy<(string ResourceProvider, string Os)> environment;
        private readonly string ciKey;
        private readonly string version;
        private readonly Meter featureMeter = new (StatsbeatConstants.FeatureStatsbeatMeterName, "1.0");

        private StatsbeatFeatures observedFeatures = StatsbeatFeatures.None;

        private FeatureMetricEmissionHelper(string ciKey, string version)
            : this(ciKey, version, GetVmMetadata)
        {
        }

        // Test seam: lets tests observe when (and whether) the IMDS lookup happens.
        internal FeatureMetricEmissionHelper(string ciKey, string version, Func<Dictionary<string, object>> getVmMetadata)
        {
            if (getVmMetadata == null)
            {
                throw new ArgumentNullException(nameof(getVmMetadata));
            }

            this.environment = new Lazy<(string, string)>(() => GetEnvironment(getVmMetadata));
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
            // This method can technically be called from multiple threads, and this method is not thread safe.
            // However, the consequence of a race is that we might miss a single report of a feature being used.
            // Over time, all features will be observed because this is an accumulation calculation.
            this.observedFeatures |= features;
        }

        internal Measurement<int> GetFeatureStatsbeat()
        {
            if (this.observedFeatures == StatsbeatFeatures.None)
            {
                // If no features have been observed, then skip sending the feature measurement
                return new Measurement<int>();
            }

            try
            {
                var (resourceProvider, os) = this.environment.Value;
                return
                    new Measurement<int>(1,
                        new ("rp", resourceProvider),
                        new ("attach", "Manual"),
                        new ("cikey", this.ciKey),
                        new ("feature", (ulong)this.observedFeatures),
                        new ("type", 0), // 0 = feature, 1 = instrumentation scopes
                        new ("os", os),
                        new ("language", "dotnet"),
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
                        var responseString = httpClient.GetStringAsync(new Uri(StatsbeatConstants.AMSUrl));
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

        private static (string ResourceProvider, string Os) GetEnvironment(Func<Dictionary<string, object>> getVmMetadata)
        {
            var os = GetOs();
            var resourceProvider = GetResourceProvider(ref os, getVmMetadata);
            return (resourceProvider, os);
        }

        /// <summary>
        /// Inspect environment variables and VM instance metadata service (IMDS) for the azure resource provider name (eg., "aks").
        ///
        /// As a side effect, also updates <paramref name="os"/> if that info is available from IMDS.
        /// </summary>
        /// <param name="getVmMetadata">IMDS lookup.</param>
        /// <param name="os">the platform-observed OS, overridden by IMDS when available.</param>
        /// <returns>the resource provider.</returns>
        private static string GetResourceProvider(ref string os, Func<Dictionary<string, object>> getVmMetadata)
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

            var vmMetadata = getVmMetadata();

            if (vmMetadata != null)
            {
                if (vmMetadata.TryGetValue("osType", out var osType) && osType is string)
                {
                    // osType takes precedence over the platform-observed OS.
#pragma warning disable CA1308 // Normalize strings to uppercase
                    os = (osType as string).ToLowerInvariant();
#pragma warning restore CA1308 // Normalize strings to uppercase
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
