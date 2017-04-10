using System;
using System.Configuration;
using System.Web.Http;

using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace TestApp45
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;

    public class WebApiApplication : System.Web.HttpApplication
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Justification = "Necessary for .NET CLR Memory counters to start reporting process ID.")]
        protected void Application_Start()
        {
            // necessary for .NET CLR Memory counters to start reporting process ID
            GC.Collect();

            var setting = ConfigurationManager.AppSettings["TestApp.SendTelemetyIntemOnAppStart"];
            if (false == string.IsNullOrWhiteSpace(setting) && true == bool.Parse(setting))
            {
                new TelemetryClient().TrackTrace("Application_Start");
            }

            GlobalConfiguration.Configure(WebApiConfig.Register);

            PerformanceCollectorModule perfModule = InitializePerformanceCollectionModule();

            QuickPulseTelemetryModule quickPulseModule = InitializeQuickPulseModule();

            TelemetryModules.Instance.Modules.Add(perfModule);
            TelemetryModules.Instance.Modules.Add(quickPulseModule);
        }

        private static QuickPulseTelemetryModule InitializeQuickPulseModule()
        {
            var quickPulseModule = new QuickPulseTelemetryModule();

            quickPulseModule.QuickPulseServiceEndpoint = "http://localhost:4555/QuickPulseService.svc/";

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
            var perfModule = new PerformanceCollectorModule();

            // we're running under IIS Express, so override the default behavior designed to prevent a deadlock
            perfModule.EnableIISExpressPerformanceCounters = true;

            // set test-friendly timings
            perfModule.CollectionPeriod = TimeSpan.FromMilliseconds(10);
            perfModule.DefaultCounters.Add(new PerformanceCounterCollectionRequest(@"\Memory\Available Bytes", @"\Memory\Available Bytes"));
            perfModule.DefaultCounters.Add(new PerformanceCounterCollectionRequest(@"Will not parse;\Does\NotExist", @"Will not parse;\Does\NotExist"));

            perfModule.Counters.Add(new PerformanceCounterCollectionRequest(@"Will not parse", "Custom counter - will not parse"));

            perfModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Does\NotExist", "Custom counter - does not exist"));
            perfModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Process(??APP_WIN32_PROC??)\IDontExist", "Custom counter with placeholder - does not exist"));

            perfModule.Counters.Add(new PerformanceCounterCollectionRequest(@"\Process(??APP_WIN32_PROC??)\Handle Count", "Custom counter one"));

            perfModule.Counters.Add(
                new PerformanceCounterCollectionRequest(@"\ASP.NET Applications(??APP_W3SVC_PROC??)\Anonymous Requests/Sec", "Custom counter two"));

            perfModule.Initialize(TelemetryConfiguration.Active);
            return perfModule;
        }
    }
}
