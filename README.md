![Build](https://mseng.visualstudio.com/DefaultCollection/_apis/public/build/definitions/96a62c4a-58c2-4dbb-94b6-5979ebc7f2af/2637/badge) 
[![codecov.io](https://codecov.io/github/Microsoft/ApplicationInsights-dotnet-logging/coverage.svg?branch=develop)](https://codecov.io/github/Microsoft/ApplicationInsights-dotnet-logging?branch=develop)

## Nuget packages

- For ILogger:
 [Microsoft.Extensions.Logging.ApplicationInsights](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.Extensions.Logging.ApplicationInsights.svg)](https://www.nuget.org/packages/Microsoft.Extensions.Logging.ApplicationInsights/)
- For NLog:
 [Microsoft.ApplicationInsights.NLogTarget](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)
- For Log4Net: [Microsoft.ApplicationInsights.Log4NetAppender](http://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.Log4NetAppender.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.Log4NetAppender/)
- For System.Diagnostics: [Microsoft.ApplicationInsights.TraceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.TraceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.TraceListener/)
- [Microsoft.ApplicationInsights.DiagnosticSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.DiagnosticSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.DiagnosticSourceListener/)
- [Microsoft.ApplicationInsights.EtwCollector](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EtwCollector.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EtwCollector/)
- [Microsoft.ApplicationInsights.EventSourceListener](http://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.EventSourceListener.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.EventSourceListener/)

Application Insights logging adapters. 
==============================

If you use NLog, log4Net or System.Diagnostics.Trace for diagnostic tracing in your  application, you can have your logs sent to Application Insights, where you can explore and search them. Your logs will be merged with the other telemetry coming from your application, so that you can identify the traces associated with servicing each user request, and correlate them with other events and exception reports.

Read more:
- [Microsoft Docs: "Explore .NET trace logs in Application Insights"](https://docs.microsoft.com/azure/application-insights/app-insights-asp-net-trace-logs)
- [Microsoft Docs: "Diagnose sudden changes in your app telemetry"](https://docs.microsoft.com/azure/application-insights/app-insights-analytics-diagnostics#trace)

## ILogger
See [this](https://github.com/Microsoft/ApplicationInsights-dotnet-logging/tree/develop/src/ILogger/Readme.md).

Console Application
Following shows a sample Console Application configured to send ILogger traces to application insights.

```
class Program
    {
        static void Main(string[] args)
        {
            // Create DI container.
            IServiceCollection services = new ServiceCollection();
            
            // Add the logging pipelines to use. We are using Console and AI and configuring them both.
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddApplicationInsights("--YourAIKeyHere--");

                loggingBuilder.AddConsole();
            });

            // Build ServiceProvider.
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Begin a new scope. This is optional. Epecially in case of AspNetCore request info is already
            // present in scope.
            using (logger.BeginScope(new Dictionary<string, object> { { "Method", nameof(Main) } }))
            {
                logger.LogInformation("Logger is working");
            }
        }
    }
```

Asp.Net Core Application
Following shows a sample Asp.Net Core Application configured to send ILogger traces to application insights. This example can be
followed to send ILogger traces from Program.cs, Startup.cs or any other Contoller/Application Logic.

```
public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("From Program. Running the host now.."); // This will be picked up up by AI
            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()                
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders(); //optionally removing all other logging providers.
                    logging.AddApplicationInsights("ikeyhere");
					// Optional: Apply filters to configure LogLevel Trace or above is sent to ApplicationInsights for all
					// categories.
					logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
					// Additional filtering For category starting in "Microsoft",
					// only Warning or above will be sent to Application Insights.
                    logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);
                })
                .Build();
    }
```

```
public class Startup
    {
        private readonly ILogger _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);                         

             _logger.LogInformation("From ConfigureServices. Services.AddMVC invoked"); // This will be picked up up by AI
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                _logger.LogInformation("Configuring for Development environment");
                app.UseDeveloperExceptionPage();
            }
            else
            {
                _logger.LogInformation("Configuring for Production environment");
            }

            app.UseMvc();
        }
    }
```

```
public class ValuesController : ControllerBase
    {
        private readonly ILogger _logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            _logger = logger;
        }

        // GET api/values
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            _logger.LogInformation("This is an information trace..");
            _logger.LogWarning("This is a warning trace..");
            _logger.LogTrace("this is a Trace level message");
            return new string[] { "value1", "value2" };
        }
		...
		....
	}
```


## NLog
Application Insights NLog Target nuget package adds ApplicationInsights target in your web.config (If you use application type that does not have web.config you can install the package but you need to configure ApplicationInsights programmatically; see below). 

For more information, see [NLog Documentation](https://github.com/nlog/NLog/wiki/Configuration-API) 

- If you configure NLog though web config then you just need do the following:

```csharp
// You need this only if you did not define InstrumentationKey in ApplicationInsights.config
TelemetryConfiguration.Active.InstrumentationKey = "Your_Resource_Key";

Logger logger = LogManager.GetLogger("Example");

logger.Trace("trace log message");
```

- If you configure NLog programmatically than create Application Insights target in code and add it to your other targets:

```csharp
var config = new LoggingConfiguration();

ApplicationInsightsTarget target = new ApplicationInsightsTarget();
// You need this only if you did not define InstrumentationKey in ApplicationInsights.config or want to use different instrumentation key
target.InstrumentationKey = "Your_Resource_Key";

LoggingRule rule = new LoggingRule("*", LogLevel.Trace, target);
config.LoggingRules.Add(rule);

LogManager.Configuration = config;

Logger logger = LogManager.GetLogger("Example");

logger.Trace("trace log message");
``` 



## Log4Net

Application Insights Log4Net adapter nuget modifies web.config and adds Application Insights Appender.

For more information, see [Log4Net Configuration](https://logging.apache.org/log4net/release/manual/configuration.html)

```csharp
// You do not need this if you have instrumentation key in the ApplicationInsights.config
TelemetryConfiguration.Active.InstrumentationKey = "Your_Resource_Key";

log4net.Config.XmlConfigurator.Configure();
var logger = LogManager.GetLogger(this.GetType());

logger.Info("Message");
logger.Warn("A warning message");
logger.Error("An error message");
```

## System.Diagnostics

Microsoft.ApplicationInsights.TraceListener nuget package modifies web.config and adds application insights listener. 

For more information, see ["Microsoft Docs: "Tracing and Instrumenting Applications"](https://docs.microsoft.com/dotnet/framework/debug-trace-profile/tracing-and-instrumenting-applications)

```xml
<configuration>
  <system.diagnostics>
    <trace>
      <listeners>
        <add name="myAppInsightsListener" type="Microsoft.ApplicationInsights.TraceListener.ApplicationInsightsTraceListener, Microsoft.ApplicationInsights.TraceListener" />
      </listeners>
    </trace>
  </system.diagnostics>
</configuration>
```

If your application type does not have web.config, add listener programmatically or in the configuration file appropriate to your application type

```csharp
// You do not need this if you have instrumentation key in the ApplicationInsights.config
TelemetryConfiguration.Active.InstrumentationKey = "Your_Resource_Key";
System.Diagnostics.Trace.TraceWarning("Slow response - database01");

``` 


## EventSource

`EventSourceTelemetryModule` allows you to configure EventSource events to be sent to Application Insights as traces. 

For more information, see [Microsoft Docs: "Using EventSource Events"](https://docs.microsoft.com/azure/application-insights/app-insights-asp-net-trace-logs#using-eventsource-events).


## ETW

`EtwCollectorTelemetryModule` allows you to configure events from ETW providers to be sent to Application Insights as traces. 

For more information, see [Microsoft Docs: "Using ETW Events"](https://docs.microsoft.com/azure/application-insights/app-insights-asp-net-trace-logs#using-etw-events).


## DiagnosticSource

You can configure `System.Diagnostics.DiagnosticSource` events to be sent to Application Insights as traces.

For more information, see [CoreFX: "Diagnostic Source Users Guide"](https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/DiagnosticSourceUsersGuide.md).

To enable, edit the `TelemetryModules` section of the ApplicationInsights.config file:

```xml
<Add Type="Microsoft.ApplicationInsights.DiagnsoticSourceListener.DiagnosticSourceTelemetryModule, Microsoft.ApplicationInsights.DiagnosticSourceListener">
      <Sources>
        <Add Name="MyDiagnosticSourceName" />
      </Sources>
 </Add>
 ```

