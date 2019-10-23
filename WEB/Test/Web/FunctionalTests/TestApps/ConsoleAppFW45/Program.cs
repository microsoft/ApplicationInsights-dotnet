namespace ConsoleAppFW45
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                TelemetryClient client = new TelemetryClient(TelemetryConfiguration.Active);
                client.TrackTrace("One trace is sent to make sure that SDK is initialized.");
                
                string param = args[0];

                switch (param)
                {
                    case "unhandled" : 
                        GenerateUnhandledException();
                        break;
                    case "unobserved" :
                        GenerateUnobservedException(client);
                        break;
                }
            }
            else
                throw new ArgumentException("One parameter is required");
        }

        private static void GenerateUnhandledException()
        {
            throw new Exception("Fatal exception");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Justification = "This forces exception to become unobserved.")]
        private static void GenerateUnobservedException(TelemetryClient client)
        {
            Task.Factory.StartNew(() => { throw new Exception(); });
            
            HardWork(TimeSpan.FromSeconds(5));
           
            // This forces exception to become unobserved
            GC.Collect(2, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            HardWork(TimeSpan.FromSeconds(5));

            client.Flush();

            HardWork(TimeSpan.FromSeconds(5));
        }

        private static void HardWork(TimeSpan timeout)
        {
            DateTime endTime = DateTime.Now + timeout;

            while (DateTime.Now < endTime)
            {
                Thread.Sleep(5);
            }
        }
    }
}
