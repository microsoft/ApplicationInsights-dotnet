using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using ServerTelemetryChannel = Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel;

namespace PerfTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch s1 = new Stopwatch();
            s1.Start();

            var activeConfiguration = TelemetryConfiguration.Active;
            activeConfiguration.InstrumentationKey = "21534102-7318-4355-b38e-a00e73b1c1ce";

            var channel = new ServerTelemetryChannel();
            channel.StorageFolder = @"D:\home\site\aidata";
            channel.MaxTransmissionSenderCapacity = 10;
            channel.MaxTransmissionBufferCapacity = 10485760;
            activeConfiguration.TelemetryChannel = channel;
            channel.Initialize(activeConfiguration);
            
            var telemetryClient = new TelemetryClient(activeConfiguration);

            
            // WaitUntilFolderClean();
            
            DoPerf(telemetryClient);
        }

        private static void WaitUntilFolderClean()
        {
            Stopwatch s2 = new Stopwatch();
            s2.Start();

            while (true)
            {
                var files = Directory.GetFiles(@"D:\home\site\aidata");
                Console.WriteLine("File Count" + files.Length);
                Console.WriteLine("Time so far: " + s2.Elapsed);
                if (files.Length < 10)
                {
                    break;
                }
                Thread.Sleep(5000);
            }

            Console.WriteLine("File clear took: " + s2.Elapsed);
        }

        private static void DoPerf(TelemetryClient telemetryClient)
        {
            int IterationMax = 50;
            int TaskCount = Environment.ProcessorCount;

            long[] runs = new long[IterationMax - 1];


            Stopwatch sw;

            for (int iter = 0; iter < IterationMax; iter++)
            {
                sw = new Stopwatch();
                sw.Start();
                Task[] tasks = new Task[TaskCount];

                for (int i = 0; i < TaskCount; i++)
                {
                    tasks[i] = new Task(() =>
                    {
                        for (int j = 0; j < 2500; j++)
                        {
                            var req = new RequestTelemetry("Http", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                                "200", (j % 2 == 0) ? true : false);
                            req.Url = new Uri("http://www.google.com");

                            var dep = new DependencyTelemetry("Http", "MyTarget", "bing.com", "bing.com?url=true", DateTimeOffset.Now, TimeSpan.FromMilliseconds(200),
                                "200", (j % 2 == 0) ? true : false);

                            telemetryClient.TrackRequest(req);
                            telemetryClient.TrackDependency(dep);
                        }
                    });
                }

                for (int i = 0; i < TaskCount; i++)
                {
                    tasks[i].Start();
                }

                Task.WaitAll(tasks);

                sw.Stop();
                Console.WriteLine(sw.ElapsedMilliseconds);

                if (iter > 0)
                {
                    runs[iter - 1] = sw.ElapsedMilliseconds;
                }
            }

            Console.WriteLine("Avge" + runs.Average());
        }
    }
    

    internal class MyTelemetryInitializer : ITelemetryInitializer
    {
        private string currentValue;

        public void Initialize(ITelemetry telemetry)
        {
            currentValue = "mystring";
            for (int i = 0; i < 100; i++)
            {
                telemetry.Context.Device.Id = currentValue;
            }
        }
    }

    internal class MyChannel : ITelemetryChannel
    {
        public MyChannel()
        {
        }

        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }

        public void Dispose()
        {

        }

        public void Flush()
        {

        }

        public void Send(ITelemetry item)
        {
            
        }
    }
}
