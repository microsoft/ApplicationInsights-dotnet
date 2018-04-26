namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// <see cref="IStartupFilter"/> implementation that initialized ApplicationInsights services on application startup
    /// </summary>
    internal class ApplicationInsightsStartupFilter : IStartupFilter
    {
        /// <inheritdoc/>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var tc = app.ApplicationServices.GetService<TelemetryConfiguration>();
                var applicationInsightsDebugLogger = app.ApplicationServices.GetService<ApplicationInsightsDebugLogger>();
                next(app);
            };
        }
    }
}