using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DependencyCollector;

namespace E2ETestAppCore20
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
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();

            var teleConfig = TelemetryConfiguration.Active;
            teleConfig.TelemetryChannel.DeveloperMode = true;

            // Fake endpoint.
            teleConfig.TelemetryChannel.EndpointAddress = "http://172.20.45.192/api/Data/PushItem";
            teleConfig.InstrumentationKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8";

            new DependencyTrackingTelemetryModule().Initialize(TelemetryConfiguration.Active);
        }
    }
}
