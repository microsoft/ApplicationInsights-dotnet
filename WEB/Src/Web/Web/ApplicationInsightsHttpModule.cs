namespace Microsoft.ApplicationInsights.Web
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Web;
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
    /// Platform agnostic module for web application instrumentation.
    /// </summary>
    public sealed class ApplicationInsightsHttpModule : IHttpModule
    {
        // Static fields to track initialization across all module instances
        private static readonly object StaticLockObject = new object();
        private static int initializationCount = 0;
        private static TelemetryConfiguration sharedTelemetryConfiguration;
        private static TelemetryClient sharedTelemetryClient;
        private static bool isInitialized = false;

        private TelemetryConfiguration telemetryConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInsightsHttpModule"/> class.
        /// </summary>
        public ApplicationInsightsHttpModule()
        {
        }

        /// <summary>
        /// Initializes module for a given application.
        /// </summary>
        /// <param name="context">HttpApplication instance.</param>
        public void Init(HttpApplication context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            lock (StaticLockObject)
            {
                initializationCount++;

                // Only initialize the shared configuration once per AppDomain
                if (!isInitialized)
                {
                    sharedTelemetryConfiguration = TelemetryConfiguration.CreateDefault();

                    sharedTelemetryConfiguration.ExtensionVersion = VersionUtils.ExtensionLabelShimWeb + VersionUtils.GetVersion(typeof(ApplicationInsightsExtensions));
                    
                    // Read all configuration options from applicationinsights.config
                    ApplicationInsightsConfigOptions configOptions = ApplicationInsightsConfigurationReader.GetConfigurationOptions();
                    
                    if (configOptions != null)
                    {
                        // Apply Group 1: Direct TelemetryConfiguration properties (before Build)
                        if (!string.IsNullOrEmpty(configOptions.ConnectionString))
                        {
                            sharedTelemetryConfiguration.ConnectionString = configOptions.ConnectionString;
                            WebEventSource.Log.ConnectionStringLoadedFromConfig(configOptions.ConnectionString);
                        }
                        else
                        {
                            WebEventSource.Log.NoConnectionStringFoundInConfig();
                        }

                        if (configOptions.DisableTelemetry.HasValue)
                        {
                            sharedTelemetryConfiguration.DisableTelemetry = configOptions.DisableTelemetry.Value;
                        }

                        if (configOptions.TracesPerSecond.HasValue)
                        {
                            sharedTelemetryConfiguration.TracesPerSecond = configOptions.TracesPerSecond.Value;
                        }

                        if (configOptions.SamplingRatio.HasValue)
                        {
                            sharedTelemetryConfiguration.SamplingRatio = configOptions.SamplingRatio.Value;
                            if (!configOptions.TracesPerSecond.HasValue)
                            {
                                sharedTelemetryConfiguration.TracesPerSecond = null;
                            }
                        }

                        if (!string.IsNullOrEmpty(configOptions.StorageDirectory))
                        {
                            sharedTelemetryConfiguration.StorageDirectory = configOptions.StorageDirectory;
                        }

                        if (configOptions.DisableOfflineStorage.HasValue)
                        {
                            sharedTelemetryConfiguration.DisableOfflineStorage = configOptions.DisableOfflineStorage.Value;
                        }

                        if (configOptions.EnableTraceBasedLogsSampler.HasValue)
                        {
                            sharedTelemetryConfiguration.EnableTraceBasedLogsSampler = configOptions.EnableTraceBasedLogsSampler.Value;
                        }

                        // EnableQuickPulseMetricStream -> EnableLiveMetrics (TelemetryConfiguration property)
                        if (configOptions.EnableQuickPulseMetricStream.HasValue)
                        {
                            sharedTelemetryConfiguration.EnableLiveMetrics = configOptions.EnableQuickPulseMetricStream.Value;
                        }

                        if (!string.IsNullOrEmpty(configOptions.ApplicationVersion))
                        {
                            sharedTelemetryConfiguration.DefaultContext.Component.Version = configOptions.ApplicationVersion;
                        }

                        // Configure OpenTelemetry builder for properties that require OpenTelemetry API
                        sharedTelemetryConfiguration.ConfigureOpenTelemetryBuilder(
                            builder => ConfigureOpenTelemetryWithOptions(builder, configOptions));
                    }
                    else
                    {
                        WebEventSource.Log.NoConnectionStringFoundInConfig();

                        sharedTelemetryConfiguration.ConfigureOpenTelemetryBuilder(
                            builder => builder.UseApplicationInsightsAspNetTelemetry());
                    }

                    isInitialized = true;
                }

                // Use the shared configuration for this instance
                this.telemetryConfiguration = sharedTelemetryConfiguration;
            }

            // Subscribe to events (this is safe to do multiple times as ASP.NET handles duplicate subscriptions)
            context.BeginRequest += this.OnBeginRequest;
        }

        /// <summary>
        /// Required IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            // Don't dispose the shared configuration, as other instances might still be using it
            // It will be cleaned up when the AppDomain unloads

            // Note: If you need to dispose, you'd need a reference counting mechanism
        }

        /// <summary>
        /// Configures OpenTelemetry builder with options that require OpenTelemetry API access.
        /// Note: Classic ASP.NET doesn't use DI, so we can only configure things through the builder's direct API.
        /// </summary>
        private static void ConfigureOpenTelemetryWithOptions(IOpenTelemetryBuilder builder, ApplicationInsightsConfigOptions configOptions)
        {
            builder.UseApplicationInsightsAspNetTelemetry();

            // Configure AzureMonitorExporterOptions for internal properties using reflection
            // Even though classic ASP.NET doesn't use DI, the OpenTelemetry builder does internally
            builder.Services.Configure<AzureMonitorExporterOptions>(exporterOptions =>
            {
                // EnablePerformanceCounterCollectionModule -> EnablePerfCounters (internal property)
                if (configOptions.EnablePerformanceCounterCollectionModule.HasValue)
                {
                    TrySetInternalProperty(exporterOptions, "EnablePerfCounters", configOptions.EnablePerformanceCounterCollectionModule.Value);
                }

                // AddAutoCollectedMetricExtractor -> EnableStandardMetrics (internal property)
                if (configOptions.AddAutoCollectedMetricExtractor.HasValue)
                {
                    TrySetInternalProperty(exporterOptions, "EnableStandardMetrics", configOptions.AddAutoCollectedMetricExtractor.Value);
                }
            });

            // Handle EnableDependencyTrackingTelemetryModule and EnableRequestTrackingTelemetryModule - add activity filter processor
            bool enableDependencyTracking = configOptions.EnableDependencyTrackingTelemetryModule ?? true;
            bool enableRequestTracking = configOptions.EnableRequestTrackingTelemetryModule ?? true;

            // Only add processor if either feature is disabled
            if (!enableDependencyTracking || !enableRequestTracking)
            {
                // Use WithTracing to get TracerProviderBuilder which has AddProcessor method
                builder.WithTracing(tracerBuilder =>
                {
                    tracerBuilder.AddProcessor(new ActivityFilterProcessor(enableDependencyTracking, enableRequestTracking));
                });
            }

            // Handle ApplicationVersion - add to resource attributes
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
            catch
            {
                // Silently ignore if property doesn't exist or can't be set
                // This allows forward/backward compatibility across versions
            }
        }

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            var client = Volatile.Read(ref sharedTelemetryClient);
            if (client == null)
            {
                lock (StaticLockObject)
                {
                    client = sharedTelemetryClient;
                    if (client == null)
                    {
                        var newClient = new TelemetryClient(this.telemetryConfiguration);
                        Volatile.Write(ref sharedTelemetryClient, newClient);
                        client = newClient;
                    }
                }
            }
        }
    }
}
