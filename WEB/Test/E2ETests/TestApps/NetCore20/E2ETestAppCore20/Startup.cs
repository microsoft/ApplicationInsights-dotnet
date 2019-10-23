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
        public Startup(IHostingEnvironment env)
        {            
            var configBuilder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json");

            Configuration = configBuilder.Build();            
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppInsightsOptions>(Configuration);
            services.AddMvc();

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, 
            IOptions<AppInsightsOptions> options,
            TelemetryConfiguration configuration,
            DependencyTrackingTelemetryModule module)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
            module.Initialize(configuration);
        }
    }
}
