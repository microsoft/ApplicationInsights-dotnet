![Build](https://mseng.visualstudio.com/DefaultCollection/_apis/public/build/definitions/96a62c4a-58c2-4dbb-94b6-5979ebc7f2af/2637/badge) 
[![codecov.io](https://codecov.io/github/Microsoft/ApplicationInsights-dotnet-logging/coverage.svg?branch=develop)](https://codecov.io/github/Microsoft/ApplicationInsights-dotnet-logging?branch=develop)

## Nuget packages

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

[Application Insights Documentation](https://azure.microsoft.com/en-us/documentation/articles/app-insights-search-diagnostic-logs/#trace).

# NLog
Application Insights NLog Target nuget package adds ApplicationInsights target in your web.config (If you use application type that does not have web.config you can install the package but you need to configure ApplicationInsights programmatically; see below). 

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

[NLog Documentation](https://github.com/nlog/NLog/wiki/Configuration-API) 

# Log4Net

Application Insights Log4Net adapter nuget modifies web.config and adds Application Insights Appender.

```csharp
// You do not need this if you have instrumentation key in the ApplicationInsights.config
TelemetryConfiguration.Active.InstrumentationKey = "Your_Resource_Key";

log4net.Config.XmlConfigurator.Configure();
var logger = LogManager.GetLogger(this.GetType());

logger.Info("Message");
logger.Warn("A warning message");
logger.Error("An error message");
```

# System.Diagnostics

Microsoft.ApplicationInsights.TraceListener nuget package modifies web.config and adds application insights listener. 

```
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
