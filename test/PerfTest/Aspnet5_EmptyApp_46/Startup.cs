using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Aspnet5_EmptyApp_46
{
    public class Startup
    {
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "Request",
                    template: "{controller}/{action}/{loadTimeInMs}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
                //defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
