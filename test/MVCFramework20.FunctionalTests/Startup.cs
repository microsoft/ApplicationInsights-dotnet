using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights.Channel;
using FunctionalTestUtils;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;

namespace MVCFramework20.FunctionalTests
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var endpointAddress = new EndpointAddress();
            services.AddSingleton<EndpointAddress>(endpointAddress);

            ApplicationInsightsServiceOptions applicationInsightsOptions = new ApplicationInsightsServiceOptions();
            applicationInsightsOptions.AddAutoCollectedMetricExtractor = false;
            applicationInsightsOptions.DeveloperMode = true;
            applicationInsightsOptions.EndpointAddress = endpointAddress.ConnectionString;
            applicationInsightsOptions.InstrumentationKey = "foo";

            services.AddApplicationInsightsTelemetry(applicationInsightsOptions);
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
