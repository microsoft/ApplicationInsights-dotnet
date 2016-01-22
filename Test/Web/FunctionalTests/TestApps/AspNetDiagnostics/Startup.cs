using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(AspNetDiagnostics.Startup))]
namespace AspNetDiagnostics
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
