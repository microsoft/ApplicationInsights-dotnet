using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            TelemetryClient client = new TelemetryClient();


            client.Context.InstrumentationKey = "myikey";
            client.Context.Properties.Add("TC.Context.Property", "SomeValue");
            client.Context.GlobalProperties.Add("TC.Context.GlobalProperty", "SomeValue");
            DependencyTelemetry dep = new DependencyTelemetry("SQL", "MyDependencyTarget","MyDepndencyName", "Data for my dependency", DateTime.Now.AddMilliseconds(-300), TimeSpan.FromSeconds(2), "200", true);
            dep.Properties.Add("dep.Properties", "SomeProperty");
            client.TrackDependency(dep);

            int x = 0;
            try
            {
                int y = 10 / x;
            }
            catch(Exception ex)
            {
                client.TrackException(ex);
            }


            var met = new MetricTelemetry("mymetric", 38.09);
            client.TrackMetric(met);

            //var jsonWriter = new JsonSerializationWriter(new StreamWriter("d:\\cijo.txt"));
            //met.Serialize(jsonWriter);



            //jsonWriter.Flush();
        }
    }
}
