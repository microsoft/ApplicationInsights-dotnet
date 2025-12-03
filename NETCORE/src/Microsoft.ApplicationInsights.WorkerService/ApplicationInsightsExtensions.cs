namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Azure.Monitor.OpenTelemetry.Exporter;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.WorkerService;
    using Microsoft.ApplicationInsights.WorkerService.Implementation.Tracing;
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
        public static IServiceCollection AddApplicationInsightsTelemetryWorkerService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddApplicationInsightsTelemetryWorkerService(options => AddTelemetryConfiguration(configuration, options));
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
        public static IServiceCollection AddApplicationInsightsTelemetryWorkerService(
            this IServiceCollection services,
            Action<ApplicationInsightsServiceOptions> options)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
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
        public static IServiceCollection AddApplicationInsightsTelemetryWorkerService(
            this IServiceCollection services,
            ApplicationInsightsServiceOptions options)
        {
            services.AddApplicationInsightsTelemetryWorkerService();
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
        public static IServiceCollection AddApplicationInsightsTelemetryWorkerService(this IServiceCollection services)
        {
            try
            {
                if (!IsApplicationInsightsAdded(services))
                {
                    services.AddOpenTelemetry()
                            .WithApplicationInsights()
                            .UseApplicationInsightsTelemetry();

                    AddTelemetryConfigAndClient(services);
                }

                return services;
            }
            catch (Exception e)
            {
                WorkerServiceEventSource.Instance.LogError(e.ToInvariantString());
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
                .AddAttributes(new[] { new KeyValuePair<string, object>("telemetry.distro.name", "Microsoft.ApplicationInsights.WorkerService") })
                .AddAzureAppServiceDetector()
                .AddAzureVMDetector();

            builder.ConfigureResource(configureResource);

            builder.WithTracing(b => b
                            .AddSource("Azure.*")
                            .AddSqlClientInstrumentation()
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
                            }));

            builder.WithMetrics(b => b.AddHttpClientMetrics());

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
                .Configure<IOptions<ApplicationInsightsServiceOptions>>((exporterOptions, aiOptions) =>
                {
                    var serviceOptions = aiOptions.Value;

                    // Copy connection string to Azure Monitor Exporter
                    if (!string.IsNullOrEmpty(serviceOptions.ConnectionString))
                    {
                        exporterOptions.ConnectionString = serviceOptions.ConnectionString;
                    }

                    if (!serviceOptions.EnableAdaptiveSampling)
                    {
                        exporterOptions.SamplingRatio = 1.0F;
                    }

                    if (serviceOptions.EnableQuickPulseMetricStream)
                    {
                        exporterOptions.EnableLiveMetrics = true;
                    }
                });

            builder.UseAzureMonitorExporter();

            return builder;
        }

        private static MeterProviderBuilder AddHttpClientMetrics(this MeterProviderBuilder meterProviderBuilder)
        {
            return Environment.Version.Major >= 8 ?
                meterProviderBuilder.AddMeter("System.Net.Http")
                : meterProviderBuilder.AddHttpClientInstrumentation();
        }
    }
}
