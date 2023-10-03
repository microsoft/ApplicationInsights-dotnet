using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var key = "APPLICATIONINSIGHTS_LOG_DIAGNOSTICS";
            var value = @"C:\home\LogFiles\SelfDiagnostics";
            //var value = "C:\\home\\LogFiles\\Application\\Functions\\";
            Environment.SetEnvironmentVariable(key, value);

            Console.WriteLine("Hello, World!");
            Console.WriteLine(Directory.GetCurrentDirectory());

            TelemetryClient client = new TelemetryClient();

            for (int i = 0; i < 10000; i++)
                client.TrackTrace("sd" + i);
            client.Flush();
            Thread.Sleep(10000);
        }
    }
}