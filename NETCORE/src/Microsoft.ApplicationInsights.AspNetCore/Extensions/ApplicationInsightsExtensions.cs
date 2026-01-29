namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.AspNetCore;
    using Microsoft.ApplicationInsights.AspNetCore.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.AspNetCore.Extensions;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Internal;
    using Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Options;
    using OpenTelemetry;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> that allow adding Application Insights services to application.
    /// </summary>
    public static partial class ApplicationInsightsExtensions
    {
        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">Configuration to use for sending telemetry.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetry(options => AddTelemetryConfiguration(configuration, options));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The action used to configure the options.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure(options);
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="options">The options instance used to configure with.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(
            this IServiceCollection services,
            ApplicationInsightsServiceOptions options)
        {
            services.AddApplicationInsightsTelemetry();
            services.Configure((ApplicationInsightsServiceOptions o) => options.CopyPropertiesTo(o));
            return services;
        }

        /// <summary>
        /// Adds Application Insights services into service collection.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <returns>
        /// The <see cref="IServiceCollection"/>.
        /// </returns>
        public static IServiceCollection AddApplicationInsightsTelemetry(this IServiceCollection services)
        {
            try
            {
                if (!IsApplicationInsightsAdded(services))
                {
                    // Register the default configuration options to automatically read from appsettings.json
                    services.AddOptions<ApplicationInsightsServiceOptions>()
                        .Configure<IConfiguration>((options, config) =>
                        {
                            AddTelemetryConfiguration(config, options);
                        });

                    services.AddOpenTelemetry()
                        .WithApplicationInsights()
                        .UseApplicationInsightsTelemetry();

                    AddTelemetryConfigAndClient(services, VersionUtils.ExtensionLabelShimAspNetCore + VersionUtils.GetVersion(typeof(ApplicationInsightsExtensions)));
                    services.AddSingleton<IJavaScriptSnippet, JavaScriptSnippet>();
                    services.AddSingleton<JavaScriptSnippet>();
                }

                return services;
            }
            catch (Exception e)
            {
                AspNetCoreEventSource.Instance.FailedToAddTelemetry(e.ToInvariantString());
                return services;
            }
        }

        internal static IOpenTelemetryBuilder UseApplicationInsightsTelemetry(this IOpenTelemetryBuilder builder, Action<ApplicationInsightsServiceOptions> configureApplicationInsights = null)
        {
            if (builder.Services == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(builder.Services));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            if (configureApplicationInsights != null)
            {
                builder.Services.Configure(configureApplicationInsights);
            }

            Action<ResourceBuilder> configureResource = (r) => r
                .AddAttributes(new[]
                {
                    new KeyValuePair<string, object>("telemetry.distro.name", "Microsoft.ApplicationInsights.AspNetCore"),
                    new KeyValuePair<string, object>("telemetry.distro.version", VersionUtils.GetVersion(typeof(ApplicationInsightsExtensions))),
                })
                .AddAzureAppServiceDetector()
                .AddAzureVMDetector()
                .AddDetector(sp => new AspNetCoreEnvironmentResourceDetector(sp.GetService<IConfiguration>()));

            builder.ConfigureResource(configureResource);

            builder.WithTracing(b => b
                            .AddSource("Azure.*")
                            .AddSqlClientInstrumentation()
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation(o => o.FilterHttpRequestMessage = (_) =>
                            {
                                // Azure SDKs create their own client span before calling the service using HttpClient
                                // In this case, we would see two spans corresponding to the same operation
                                // 1) created by Azure SDK 2) created by HttpClient
                                // To prevent this duplication we are filtering the span from HttpClient
                                // as span from Azure SDK contains all relevant information needed.
                                var parentActivity = Activity.Current?.Parent;
                                if (parentActivity != null && parentActivity.Source.Name.Equals("Azure.Core.Http", StringComparison.Ordinal))
                                {
                                    return false;
                                }

                                return true;
                            })
                            .AddProcessor<ActivityFilterProcessor>());

            // Register ActivityFilterProcessor in DI
            builder.Services.AddSingleton<ActivityFilterProcessor>();

            builder.WithMetrics(b => b.AddHttpClientAndServerMetrics());

            builder.Services.AddOptions<ApplicationInsightsServiceOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    // This is a temporary workaround for hotfix GHSA-vh2m-22xx-q94f.
                    // https://github.com/open-telemetry/opentelemetry-dotnet/security/advisories/GHSA-vh2m-22xx-q94f
                    // We are disabling the workaround set by OpenTelemetry.Instrumentation.AspNetCore v1.8.1 and OpenTelemetry.Instrumentation.Http v1.8.1.
                    // The OpenTelemetry Community is deciding on an official stance on this issue and we will align with that final decision.
                    // TODO: FOLLOW UP ON: https://github.com/open-telemetry/semantic-conventions/pull/961 (2024-04-26)
                    if (config["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"] == null)
                    {
                        config["ASPNETCORE_DISABLE_URL_QUERY_REDACTION"] = Boolean.TrueString;
                    }

                    if (config["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"] == null)
                    {
                        config["HTTPCLIENT_DISABLE_URL_QUERY_REDACTION"] = Boolean.TrueString;
                    }

                    // If connection string is not set in the options, try to get it from configuration.
                    if (string.IsNullOrWhiteSpace(options.ConnectionString) && config["APPLICATIONINSIGHTS_CONNECTION_STRING"] != null)
                    {
                        options.ConnectionString = config["APPLICATIONINSIGHTS_CONNECTION_STRING"];
                    }
                });

            // Configure Azure Monitor Exporter with connection string and sampling from ApplicationInsightsServiceOptions
            builder.Services.AddOptions<AzureMonitorExporterOptions>()
                .Configure<IOptions<ApplicationInsightsServiceOptions>, TelemetryConfiguration, IConfiguration>((exporterOptions, aiOptions, telemetryConfig, config) =>
                {
                    var serviceOptions = aiOptions.Value;

                    // Set OTEL_SDK_DISABLED in configuration if DisableTelemetry is true
                    if (telemetryConfig.DisableTelemetry)
                    {
                        config["OTEL_SDK_DISABLED"] = "true";
                    }

                    if (!string.IsNullOrEmpty(telemetryConfig.StorageDirectory))
                    {
                        exporterOptions.StorageDirectory = telemetryConfig.StorageDirectory;
                    }

                    if (telemetryConfig.DisableOfflineStorage.HasValue)
                    {
                        exporterOptions.DisableOfflineStorage = telemetryConfig.DisableOfflineStorage.Value;
                    }

                    if (serviceOptions.EnableTraceBasedLogsSampler.HasValue)
                    {
                        exporterOptions.EnableTraceBasedLogsSampler = serviceOptions.EnableTraceBasedLogsSampler.Value;
                    }
                    
                    // Copy connection string to Azure Monitor Exporter
                    if (!string.IsNullOrEmpty(serviceOptions.ConnectionString))
                    {
                        exporterOptions.ConnectionString = serviceOptions.ConnectionString;
                    }

                    // Copy credential to Azure Monitor Exporter
                    if (serviceOptions.Credential != null)
                    {
                        exporterOptions.Credential = serviceOptions.Credential;
                    }

                    if (serviceOptions.EnableQuickPulseMetricStream)
                    {
                        exporterOptions.EnableLiveMetrics = true;
                    }
                    else
                    {
                        exporterOptions.EnableLiveMetrics = false;
                    }

                    if (serviceOptions.TracesPerSecond.HasValue)
                    {
                        if (serviceOptions.TracesPerSecond.Value >= 0)
                        {
                            exporterOptions.TracesPerSecond = serviceOptions.TracesPerSecond.Value;
                        }
                        else 
                        {
                            AspNetCoreEventSource.Instance.InvalidTracesPerSecondConfigured(serviceOptions.TracesPerSecond.Value);
                        }
                    }

                    if (serviceOptions.SamplingRatio.HasValue)
                    {
                        if (serviceOptions.SamplingRatio.Value >= 0.0f && serviceOptions.SamplingRatio.Value <= 1.0f) 
                        {
                            exporterOptions.SamplingRatio = serviceOptions.SamplingRatio.Value;
                            if (!serviceOptions.TracesPerSecond.HasValue)
                            {
                                exporterOptions.TracesPerSecond = null;
                            }
                        }
                        else
                        {
                            AspNetCoreEventSource.Instance.InvalidSamplingRatioConfigured(serviceOptions.SamplingRatio.Value);
                        }
                    }

                    // Configure standard metrics and performance counter collection using reflection
                    // Only set when false since the default is true
                    if (!serviceOptions.AddAutoCollectedMetricExtractor)
                    {
                        TrySetInternalProperty(exporterOptions, "EnableStandardMetrics", false);
                    }

                    if (!serviceOptions.EnablePerformanceCounterCollectionModule)
                    {
                        TrySetInternalProperty(exporterOptions, "EnablePerfCounters", false);
                    }
                });

            builder.UseAzureMonitorExporter();

            return builder;
        }

        private static MeterProviderBuilder AddHttpClientAndServerMetrics(this MeterProviderBuilder meterProviderBuilder)
        {
            return Environment.Version.Major >= 8 ?
                meterProviderBuilder.AddMeter("Microsoft.AspNetCore.Hosting").AddMeter("System.Net.Http")
                : meterProviderBuilder.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
        }

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
    }
}
