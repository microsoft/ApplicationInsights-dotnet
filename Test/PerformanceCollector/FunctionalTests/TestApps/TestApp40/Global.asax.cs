namespace TestApp40
{
    using System;
    using System.Configuration;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;

    public class Global : System.Web.HttpApplication
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Justification = "Necessary for .NET CLR Memory counters to start reporting process ID.")]
        protected void Application_Start(object sender, EventArgs e)
        {
            // necessary for .NET CLR Memory counters to start reporting process ID
            GC.Collect();

            var setting = ConfigurationManager.AppSettings["TestApp.SendTelemetyIntemOnAppStart"];

            if (false == string.IsNullOrWhiteSpace(setting) && true == bool.Parse(setting))
            {
                new TelemetryClient().TrackTrace("Application_Start");
            }

            PerformanceCollectorModule perfModule = InitializePerformanceCollectionModule();
            
            QuickPulseTelemetryModule quickPulseModule = InitializeQuickPulseModule();
            
            TelemetryModules.Instance.Modules.Add(perfModule);
            TelemetryModules.Instance.Modules.Add(quickPulseModule);
        }

        private static QuickPulseTelemetryModule InitializeQuickPulseModule()
        {
            var quickPulseModule = new QuickPulseTelemetryModule();

            quickPulseModule.QuickPulseServiceEndpoint = "http://localhost:7655/QuickPulseService.svc/";

            QuickPulseTelemetryProcessor processor = null;
            TelemetryConfiguration.Active.TelemetryProcessorChainBuilder.Use(
                (next) =>
                    {
                        processor = new QuickPulseTelemetryProcessor(next);
                        quickPulseModule.RegisterTelemetryProcessor(processor);
                        return processor;
                    });

            TelemetryConfiguration.Active.TelemetryProcessorChainBuilder.Build();

            quickPulseModule.Initialize(TelemetryConfiguration.Active);

            return quickPulseModule;
        }

        private static PerformanceCollectorModule InitializePerformanceCollectionModule()
        {
            var module = new PerformanceCollectorModule();

            // we're running under IIS Express, so override the default behavior designed to prevent a deadlock
            module.EnableIISExpressPerformanceCounters = true;

            // set test-friendly timings
            module.CollectionPeriod = TimeSpan.FromMilliseconds(10);

            module.DefaultCounters.Add(new PerformanceCounterCollectionRequest(@"\Memory\Available Bytes", @"\Memory\Available Bytes"));
            module.DefaultCounters.Add(new PerformanceCounterCollectionRequest(@"Will not parse;\Does\NotExist", @"Will not parse;\Does\NotExist"));

            module.Counters.Add(new PerformanceCounterCollectionRequest(@"Will not parse", "Custom counter - will not parse"));

            module.Counters.Add(new PerformanceCounterCollectionRequest(@"\Does\NotExist", "Custom counter - does not exist"));
            module.Counters.Add(new PerformanceCounterCollectionRequest(@"\Process(??APP_WIN32_PROC??)\IDontExist", "Custom counter with placeholder - does not exist"));

            module.Counters.Add(new PerformanceCounterCollectionRequest(@"\Process(??APP_WIN32_PROC??)\Handle Count", "Custom counter one"));

            module.Counters.Add(
                new PerformanceCounterCollectionRequest(@"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests/Sec", "Custom counter two"));

            module.Initialize(TelemetryConfiguration.Active);

            return module;
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