using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.ApplicationInsights.AspNetCore
{
    internal class ApplicationInsightsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var appInsightsInitializer = app.ApplicationServices.GetService<ApplicationInsightInitializer>();
                appInsightsInitializer.Start();
                next(app);
            };
        }
    }
}