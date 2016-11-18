namespace Microsoft.ApplicationInsights.AspNetCore
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;

    internal class ApplicationInsightsStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                var appInsightsInitializer = app.ApplicationServices.GetService<ApplicationInsightsInitializer>();
                appInsightsInitializer.Start();
                next(app);
            };
        }
    }
}