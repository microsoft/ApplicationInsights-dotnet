using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E2ETestAppCore20
{   
    public class Program
    {
        public static string EndPointAddressFormat = "http://{0}/api/Data/PushItem";
        public static void Main(string[] args)
        {
            IWebHostBuilder builder = new WebHostBuilder()
            .UseKestrel()
            .UseContentRoot(Directory.GetCurrentDirectory())
            .UseIISIntegration()
            .UseStartup<Startup>();

            int portNumber;
            if (args.Length >= 1 && int.TryParse(args[0], out portNumber))
            {
                builder = builder.UseUrls($"http://localhost:{portNumber}/");
            }

            IWebHost host = builder.Build();

            host.Run();
        }
    }
}
