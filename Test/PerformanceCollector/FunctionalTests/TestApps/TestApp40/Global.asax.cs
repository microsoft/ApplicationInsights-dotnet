namespace TestApp40
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            var setting = ConfigurationManager.AppSettings["TestApp.SendTelemetyIntemOnAppStart"];

            if (false == string.IsNullOrWhiteSpace(setting) && true == bool.Parse(setting))
            {
                new TelemetryClient().TrackTrace("Application_Start");
            }

            var module = new PerformanceCollectorModule();

            // we're running under IIS Express, so override the default behavior designed to prevent a deadlock
            module.EnableIISExpressPerformanceCounters = true;

            // set test-friendly timings
            var privateObject = new PrivateObject(module);
            privateObject.SetField("collectionPeriod", TimeSpan.FromMilliseconds(10));
            privateObject.SetField(
               "defaultCounters",
               new List<string>() { @"\Memory\Available Bytes", @"Will not parse;\Does\NotExist" });

            module.Counters.Add(
                new PerformanceCounterCollectionRequest(@"Will not parse", "Custom counter - will not parse"));

            module.Counters.Add(
                new PerformanceCounterCollectionRequest(@"\Does\NotExist", "Custom counter - does not exist"));

            module.Counters.Add(
                new PerformanceCounterCollectionRequest(
                                          @"\Process(??APP_WIN32_PROC??)\Handle Count",
                    "Custom counter one"));

            module.Counters.Add(
                new PerformanceCounterCollectionRequest(
                                          @"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests/Sec",
                    "Custom counter two"));

            // necessary for .NET CLR Memory counters to start reporting process ID
            GC.Collect();

            module.Initialize(TelemetryConfiguration.Active);

            TelemetryModules.Instance.Modules.Add(module);
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}