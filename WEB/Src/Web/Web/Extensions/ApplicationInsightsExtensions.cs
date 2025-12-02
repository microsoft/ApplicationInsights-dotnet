namespace Microsoft.ApplicationInsights.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using OpenTelemetry;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;

    internal static class ApplicationInsightsExtensions
    {
        internal static IOpenTelemetryBuilder UseApplicationInsightsAspNetTelemetry(this IOpenTelemetryBuilder builder)
        {
            if (builder.Services == null)
            {
#pragma warning disable CA2208 // Instantiate argument exceptions correctly
                throw new ArgumentNullException(nameof(builder.Services));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly
            }

            Action<ResourceBuilder> configureResource = (r) => r
                .AddAttributes(new[] { new KeyValuePair<string, object>("telemetry.distro.name", "Microsoft.ApplicationInsights.Web") })
                .AddAzureAppServiceDetector()
                .AddAzureVMDetector();

            builder.ConfigureResource(configureResource);

            builder.WithTracing(b => b
                            .AddSource("Azure.*")
                            .AddSqlClientInstrumentation()
                            .AddAspNetInstrumentation()
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
                            // Add Application Insights Web-specific activity processors
                            .AddProcessor(new WebTestActivityProcessor())
                            .AddProcessor(new SyntheticUserAgentActivityProcessor())
                            .AddProcessor(new SessionActivityProcessor())
                            .AddProcessor(new UserActivityProcessor())
                            .AddProcessor(new AuthenticatedUserIdActivityProcessor())
                            .AddProcessor(new AccountIdActivityProcessor())
                            .AddProcessor(new ClientIpHeaderActivityProcessor()));

            builder.WithMetrics(b => b.AddAspNetInstrumentation()
                                      .AddHttpClientInstrumentation());

            /*
            builder.Services.AddOptions<ApplicationInsightsServiceOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
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
            */

            // builder.UseAzureMonitorExporter();

            return builder;
        }
    }
}
