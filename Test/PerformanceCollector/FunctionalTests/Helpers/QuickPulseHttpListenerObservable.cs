namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Text;
    using System.Threading.Tasks;
    using Functional.Serialization;
    using FunctionalTests.Helpers;

    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;

    internal class QuickPulseHttpListenerObservable : IObservable<MonitoringDataPoint>, IDisposable
    {
        private readonly HttpListener listener;
        private IObservable<MonitoringDataPoint> stream;
        
        public QuickPulseHttpListenerObservable(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
        }

        public bool FailureDetected { get; set; }

        public void Start()
        {
            this.FailureDetected = false;
            
            if (this.stream != null)
            {
                this.Stop();   
            }

            if (!this.listener.IsListening)
            {
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        public void Stop()
        {
            this.Dispose();
        }

        public IDisposable Subscribe(IObserver<MonitoringDataPoint> observer)
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException("Call QuickPulseHttpListenerObservable.Start before subscribing to the stream");
            }

            return this.stream.Subscribe(observer);
        }

        public void Dispose()
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
                this.stream = null;
            }
        }

        private IObservable<MonitoringDataPoint> CreateStream()
        {
            return Observable
                .Create<MonitoringDataPoint>
                (obs =>
                    Task.Factory.FromAsync(
                        (a, c) => this.listener.BeginGetContext(a, c),
                        ar => this.listener.EndGetContext(ar),
                        null)
                        .ToObservable()
                        .SelectMany(this.CreateSampleFromContext)
                        .Subscribe(obs)
                )
              .Repeat()
              .Publish()
              .RefCount();
        }

        private IEnumerable<MonitoringDataPoint> CreateSampleFromContext(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                var content = request.GetContent();
                var requestData = new MemoryStream(Encoding.UTF8.GetBytes(content ?? string.Empty));

                Trace.WriteLine("=>");
                Trace.WriteLine("Item received: " + content);
                Trace.WriteLine("<=");

                if (request.Url.LocalPath == "/QuickPulseService.svc/ping")
                {
                    response.AddHeader("x-ms-qps-subscribed", true.ToString());
                }
                else if (request.Url.LocalPath == "/QuickPulseService.svc/post")
                {
                    return TelemetryItemFactory.CreateQuickPulseSamples(requestData);
                }

                return new MonitoringDataPoint[0];
            }
            finally
            {
                context.Response.Close();
            }
        }
    }
}
