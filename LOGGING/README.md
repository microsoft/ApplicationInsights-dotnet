![Build](https://mseng.visualstudio.com/DefaultCollection/_apis/public/build/definitions/96a62c4a-58c2-4dbb-94b6-5979ebc7f2af/2637/badge) 

## Nuget packages
- For NLog:
 [Microsoft.ApplicationInsights.NLogTarget](http://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)
[![Nuget](https://img.shields.io/nuget/vpre/Microsoft.ApplicationInsights.NLogTarget.svg)](https://www.nuget.org/packages/Microsoft.ApplicationInsights.NLogTarget/)


## NLog

Application Insights NLog Target nuget package adds ApplicationInsights target in your web.config.

If your application does not have web.config then it can also be configured manually.

### Azure Active Directory (AAD) Authentication

To use AAD authentication with NLog, set the `Credential` property on the `ApplicationInsightsTarget`:

```csharp
using Microsoft.ApplicationInsights.NLogTarget;
using Azure.Identity;
using NLog;
using NLog.Config;

// Configure NLog programmatically with AAD
var config = new LoggingConfiguration();
var aiTarget = new ApplicationInsightsTarget
{
    Name = "aiTarget",
    ConnectionString = "InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://ingestion-endpoint.applicationinsights.azure.com/",
    Credential = new DefaultAzureCredential()  // Set AAD credential
};
config.AddTarget(aiTarget);
config.AddRule(LogLevel.Trace, LogLevel.Fatal, aiTarget);
LogManager.Configuration = config;

var logger = LogManager.GetCurrentClassLogger();
logger.Info("Using AAD authentication");
```

**Note:** You need to install the `Azure.Identity` NuGet package to use AAD authentication.

For more information, see the [Azure.Identity documentation](https://learn.microsoft.com/dotnet/api/overview/azure/identity-readme).

### Configuration

 * **Configure ApplicationInsightsTarget using NLog.config** :

```xml
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
    <extensions>
		<add assembly="Microsoft.ApplicationInsights.NLogTarget" />
    </extensions>
	<targets>
		<target xsi:type="ApplicationInsightsTarget" name="aiTarget">
			<connectionString>InstrumentationKey=YOUR_IKEY;IngestionEndpoint=https://YOUR_REGION.in.applicationinsights.azure.com/</connectionString>
			<contextproperty name="threadid" layout="${threadid}" />	<!-- Can be repeated with more context -->
		</target>
	</targets>
	<rules>
		<logger name="*" minlevel="Trace" writeTo="aiTarget" />
	</rules>
</nlog>
```

NLog allows you to configure conditional configs:

```xml
<connectionString>${configsetting:APPLICATIONINSIGHTS_CONNECTION_STRING:whenEmpty=${environment:APPLICATIONINSIGHTS_CONNECTION_STRING}}</connectionString>
```

For more information see:
- https://github.com/NLog/NLog/wiki/ConfigSetting-Layout-Renderer
- https://github.com/nlog/nlog/wiki/Environment-Layout-Renderer
- https://github.com/nlog/nlog/wiki/WhenEmpty-Layout-Renderer

* **Configure ApplicationInsightsTarget using NLog Config API** :
If you configure NLog programmatically with the [NLog Config API](https://github.com/nlog/NLog/wiki/Configuration-API), then create Application Insights target in code and add it to your other targets:

```csharp
var config = new LoggingConfiguration();

ApplicationInsightsTarget target = new ApplicationInsightsTarget();
target.ConnectionString = "InstrumentationKey=....;IngestionEndpoint=...";

LoggingRule rule = new LoggingRule("*", LogLevel.Trace, target);
config.LoggingRules.Add(rule);

LogManager.Configuration = config;

Logger logger = LogManager.GetLogger("Example");

logger.Trace("trace log message");
``` 

