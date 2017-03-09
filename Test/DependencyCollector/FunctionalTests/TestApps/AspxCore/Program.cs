namespace AspxCore
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using System;
    using System.IO;

    public class Program
    {
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
