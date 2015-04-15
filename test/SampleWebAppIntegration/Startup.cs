using System;
using FunctionalTestUtils;
using Microsoft.ApplicationInsights.AspNet;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Diagnostics.Entity;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.Logging.Console;
using SampleWebAppIntegration.Models;

namespace SampleWebAppIntegration
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Setup configuration sources.
            Configuration = new Configuration()
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();
        }

        public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Application Insights services to the services container.
            services.AddApplicationInsightsTelemetry(Configuration);

            // Add EF services to the services container.
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<ApplicationDbContext>();

            // Add Identity services to the services container.
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            // Add MVC services to the services container.
            services.AddMvc();

            // Uncomment the following line to add Web API servcies which makes it easier to port Web API 2 controllers.
            // You need to add Microsoft.AspNet.Mvc.WebApiCompatShim package to project.json
            // services.AddWebApiConventions();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory)
        {
            // Configure the HTTP request pipeline.
            // Add the console logger.
            loggerfactory.AddConsole();

            // Add Application Insights monitoring to the request pipeline as a very first middleware.
            app.UseApplicationInsightsRequestTelemetry();

            app.UseFunctionalTestTelemetryChannel();

            // Add the following to the request pipeline only in development environment.
            if (string.Equals(env.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                //app.UseBrowserLink();
                app.UseErrorPage(ErrorPageOptions.ShowAll);
                app.UseDatabaseErrorPage(DatabaseErrorPageOptions.ShowAll);
                // Set immediate delivery for Application Insights events.
                app.SetApplicationInsightsTelemetryDeveloperMode();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // send the request to the following path or controller action.
                app.UseErrorHandler("/Home/Error");
            }

            // Add Application Insights exceptions handling to the request pipeline.
            app.UseApplicationInsightsExceptionTelemetry();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline.
            app.UseIdentity();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "Home", action = "Index" });

                // Uncomment the following line to add a route for porting Web API 2 controllers.
                // routes.MapWebApiRoute("DefaultApi", "api/{controller}/{id?}");
            });
        }
    }
}
