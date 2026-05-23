// <copyright file="WebApplicationInsightsInitializer.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

[assembly: System.Web.PreApplicationStartMethod(
    typeof(Microsoft.ApplicationInsights.Web.WebApplicationInsightsInitializer),
    nameof(Microsoft.ApplicationInsights.Web.WebApplicationInsightsInitializer.Initialize))]

namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Internal;
    using Microsoft.ApplicationInsights.Web.Extensions;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.Extensions.DependencyInjection;
    using OpenTelemetry;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Configures the default <see cref="TelemetryConfiguration"/> for classic ASP.NET
    /// from <c>ApplicationInsights.config</c> before the application's <c>Application_Start</c>
    /// runs. This is invoked automatically by ASP.NET via
    /// <see cref="System.Web.PreApplicationStartMethodAttribute"/>.
    /// </summary>
    /// <remarks>
    /// This type is public only because <see cref="System.Web.PreApplicationStartMethodAttribute"/>
    /// requires the target type and method to be public. It is not intended to be called
    /// directly by user code. Customers should call <see cref="TelemetryConfiguration.CreateDefault"/>,
    /// which returns the singleton already populated by this initializer.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class WebApplicationInsightsInitializer
    {
        private static readonly object SyncRoot = new object();
        private static bool isInitialized;

        /// <summary>
        /// Initializes the default <see cref="TelemetryConfiguration"/> from
        /// <c>ApplicationInsights.config</c>. Safe to call multiple times; subsequent
        /// calls are no-ops.
        /// </summary>
        /// <remarks>
        /// This method is public only because <see cref="System.Web.PreApplicationStartMethodAttribute"/>
        /// requires it to be public. It is invoked automatically by ASP.NET and is not
        /// intended to be called directly by user code.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (isInitialized)
                {
                    return;
                }

                try
                {
                    // Materialize the singleton. Any subsequent call to
                    // TelemetryConfiguration.CreateDefault() returns this same instance.
                    TelemetryConfiguration cfg = TelemetryConfiguration.CreateDefault();

                    cfg.ExtensionVersion = VersionUtils.ExtensionLabelShimWeb
                        + VersionUtils.GetVersion(typeof(ApplicationInsightsExtensions));

                    ApplicationInsightsConfigOptions configOptions =
                        ApplicationInsightsConfigurationReader.GetConfigurationOptions();

                    if (configOptions != null)
                    {
                        ApplyConfigOptions(cfg, configOptions);
                    }
                    else
                    {
                        WebEventSource.Log.NoConnectionStringFoundInConfig();
                        cfg.ConfigureOpenTelemetryBuilder(
                            builder => builder.UseApplicationInsightsAspNetTelemetry());
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // The singleton TelemetryConfiguration was already built by user code
                    // (e.g., a TelemetryClient was constructed before PreApplicationStartMethod
                    // ran). Property setters and builder-config registrations throw in that
                    // state. There is nothing we can apply at this point — surface a
                    // diagnostic and move on. Subsequent calls become no-ops via isInitialized.
                    WebEventSource.Log.ApplicationInsightsConfigReadError(
                        "WebApplicationInsightsInitializer skipped: TelemetryConfiguration was already built. " + ex.Message);
                }

                isInitialized = true;
            }
        }

        /// <summary>
        /// Applies the values read from <c>ApplicationInsights.config</c> to the given
        /// <see cref="TelemetryConfiguration"/>.
        /// </summary>
        private static void ApplyConfigOptions(TelemetryConfiguration cfg, ApplicationInsightsConfigOptions configOptions)
        {
            // Direct TelemetryConfiguration properties (must be set before Build).
            if (!string.IsNullOrEmpty(configOptions.ConnectionString))
            {
                cfg.ConnectionString = configOptions.ConnectionString;
                WebEventSource.Log.ConnectionStringLoadedFromConfig(configOptions.ConnectionString);
            }
            else
            {
                WebEventSource.Log.NoConnectionStringFoundInConfig();
            }

            if (configOptions.DisableTelemetry.HasValue)
            {
                cfg.DisableTelemetry = configOptions.DisableTelemetry.Value;
            }

            if (configOptions.TracesPerSecond.HasValue)
            {
                cfg.TracesPerSecond = configOptions.TracesPerSecond.Value;
            }

            if (configOptions.SamplingRatio.HasValue)
            {
                cfg.SamplingRatio = configOptions.SamplingRatio.Value;
                if (!configOptions.TracesPerSecond.HasValue)
                {
                    cfg.TracesPerSecond = null;
                }
            }

            if (!string.IsNullOrEmpty(configOptions.StorageDirectory))
            {
                cfg.StorageDirectory = configOptions.StorageDirectory;
            }

            if (configOptions.DisableOfflineStorage.HasValue)
            {
                cfg.DisableOfflineStorage = configOptions.DisableOfflineStorage.Value;
            }

            if (configOptions.EnableTraceBasedLogsSampler.HasValue)
            {
                cfg.EnableTraceBasedLogsSampler = configOptions.EnableTraceBasedLogsSampler.Value;
            }

            // EnableQuickPulseMetricStream -> EnableLiveMetrics (TelemetryConfiguration property).
            if (configOptions.EnableQuickPulseMetricStream.HasValue)
            {
                cfg.EnableLiveMetrics = configOptions.EnableQuickPulseMetricStream.Value;
            }

            // Configure OpenTelemetry builder for properties that require OpenTelemetry API.
            cfg.ConfigureOpenTelemetryBuilder(
                builder => ConfigureOpenTelemetryWithOptions(builder, configOptions));
        }

        /// <summary>
        /// Configures OpenTelemetry builder with options that require OpenTelemetry API access.
        /// Note: Classic ASP.NET doesn't use DI, so we can only configure things through the builder's direct API.
        /// </summary>
        private static void ConfigureOpenTelemetryWithOptions(IOpenTelemetryBuilder builder, ApplicationInsightsConfigOptions configOptions)
        {
            builder.UseApplicationInsightsAspNetTelemetry();

            // Configure AzureMonitorExporterOptions for internal properties using reflection.
            // Even though classic ASP.NET doesn't use DI, the OpenTelemetry builder does internally.
            builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
            {
                // EnablePerformanceCounterCollectionModule -> EnablePerfCounters (internal property).
                if (configOptions.EnablePerformanceCounterCollectionModule.HasValue)
                {
                    TrySetInternalProperty(exporterOptions, "EnablePerfCounters", configOptions.EnablePerformanceCounterCollectionModule.Value);
                }

                // AddAutoCollectedMetricExtractor -> EnableStandardMetrics (internal property).
                if (configOptions.AddAutoCollectedMetricExtractor.HasValue)
                {
                    TrySetInternalProperty(exporterOptions, "EnableStandardMetrics", configOptions.AddAutoCollectedMetricExtractor.Value);
                }
            });

            // Handle EnableDependencyTrackingTelemetryModule and EnableRequestTrackingTelemetryModule - add activity filter processor.
            bool enableDependencyTracking = configOptions.EnableDependencyTrackingTelemetryModule ?? true;
            bool enableRequestTracking = configOptions.EnableRequestTrackingTelemetryModule ?? true;

            // Only add processor if either feature is disabled.
            if (!enableDependencyTracking || !enableRequestTracking)
            {
                builder.WithTracing(tracerBuilder =>
                {
                    tracerBuilder.AddProcessor(new ActivityFilterProcessor(enableDependencyTracking, enableRequestTracking));
                });
            }

            // Handle ApplicationVersion - add to resource attributes.
            if (!string.IsNullOrEmpty(configOptions.ApplicationVersion))
            {
                builder.ConfigureResource(resourceBuilder =>
                {
                    resourceBuilder.AddAttributes(new[]
                    {
                        new KeyValuePair<string, object>("service.version", configOptions.ApplicationVersion),
                    });
                });
            }
        }

        /// <summary>
        /// Tries to set an internal property on an object using reflection.
        /// Used to configure internal properties on AzureMonitorExporterOptions.
        /// </summary>
        private static void TrySetInternalProperty(object target, string propertyName, bool value)
        {
            try
            {
                var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(target, value);
                }
            }
            catch (Exception ex) when (
                ex is AmbiguousMatchException
                || ex is TargetException
                || ex is TargetInvocationException
                || ex is ArgumentException
                || ex is MethodAccessException)
            {
                // Silently ignore if the property is missing or cannot be set.
                // This allows forward/backward compatibility across exporter versions.
            }
        }
    }
}
