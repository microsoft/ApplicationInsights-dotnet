using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace E2ETestAppCore30
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
            services.Configure<AppInsightsOptions>(Configuration);
            services.AddControllers();
            services.AddSingleton(provider =>
            {
                var options = provider.GetService<IOptions<AppInsightsOptions>>();
                var telemetryConfiguration = new TelemetryConfiguration("fafa4b10-03d3-4bb0-98f4-364f0bdf5df8")
                {
                    TelemetryChannel =
                    {
                        DeveloperMode = true,
                        EndpointAddress = string.Format(Program.EndPointAddressFormat, options.Value.EndPoint)
                    }
                };

                return telemetryConfiguration;
            });
            services.AddSingleton<DependencyTrackingTelemetryModule>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
            IWebHostEnvironment env, 
            IOptions<AppInsightsOptions> options,
            TelemetryConfiguration configuration,
            DependencyTrackingTelemetryModule module)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
            
            module.Initialize(configuration);
        }
    }
}
