using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;

namespace TestAppStableSDK
{
    class Program
    {
        static void Main(string[] args)
        {
            TelemetryClient client = new TelemetryClient();


            client.Context.InstrumentationKey = "myikey";
            client.Context.Properties.Add("TC.Context.Property", "SomeValue");
            //client.Context.GlobalProperties.Add("TC.Context.GlobalProperty", "SomeValue");

            var met = new MetricTelemetry("mymetric", 38.09);
            client.TrackMetric(met);
            
            try
            {
                ThrowExc();
            }
            catch (Exception ex)
            {
                client.TrackException(ex);
            }
        }

        private static void ThrowExc()
        {
            int x = 0;
            int y = 10 / x;

        }
    }
}
