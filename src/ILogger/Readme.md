## ILogger
To learn about ILogger based logging, see [this](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging) documentation.

Console Application
Following shows a sample Console Application configured to send ILogger traces to application insights.

Packages installed
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="2.1.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.ApplicationInsights" Version="2.9.0-beta3" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="2.1.0" />
  </ItemGroup>
```

```csharp
class Program
{
    static void Main(string[] args)
    {
        // Create DI container.
        IServiceCollection services = new ServiceCollection();
            
        // Add the logging pipelines to use. We are using Application Insights only here.
        services.AddLogging(loggingBuilder =>
        {
			// Optional: Apply filters to configure LogLevel Trace or above is sent to ApplicationInsights for all
			// categories.
			loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
            loggingBuilder.AddApplicationInsights("--YourAIKeyHere--");                
        });

        // Build ServiceProvider.
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        // Begin a new scope. This is optional. Epecially in case of AspNetCore request info is already
        // present in scope.
        using (logger.BeginScope(new Dictionary<string, object> { { "Method", nameof(Main) } }))
        {
            logger.LogInformation("Logger is working"); // this will be captured by Application Insights.
        }
    }
}
```

Asp.Net Core Application
Following shows a sample Asp.Net Core Application configured to send ILogger traces to application insights. This example can be
followed to send ILogger traces from Program.cs, Startup.cs or any other Contoller/Application Logic.

```csharp
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
                logging.AddApplicationInsights("ikeyhere");
				// Optional: Apply filters to configure LogLevel Trace or above is sent to
				// ApplicationInsights for all categories.
				logging.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Trace);
				// Additional filtering For category starting in "Microsoft",
				// only Warning or above will be sent to Application Insights.
                logging.AddFilter<ApplicationInsightsLoggerProvider>("Microsoft", LogLevel.Warning);
            })
            .Build();
}
```

```csharp
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

```csharp
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
}
```

In both the above examples, the standalone package Microsoft.Extensions.Logging.ApplicationInsights was used. This, by default, would be using a bare minimum `TelemetryConfiguration` for sending data to
application insights. Bare minimum means the channel used will be `InMemoryChannel`, no sampling, and no standard TelemetryInitializers. This behavior can be overridden for a console application
as shown in below example.

Install additional package
```xml
<PackageReference Include="Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel" Version="2.9.0-beta3" />
```

Following is the relevant section showing how to override the default `TelemetryConfiguration`. This example configures `ServerTelemetryChannel`, sampling, 
and a custom TelemetryInitializer.

```csharp
			// Create DI container.
            IServiceCollection services = new ServiceCollection();
            var serverChannel = new ServerTelemetryChannel();
            services.Configure<TelemetryConfiguration>(
                        (config) =>
                        {                            
                            config.TelemetryChannel = serverChannel;
                            config.TelemetryInitializers.Add(new MyTelemetryInitalizers());
                            config.DefaultTelemetrySink.TelemetryProcessorChainBuilder.UseSampling(5);
                            serverChannel.Initialize(config);
                        }
                );

            // Add the logging pipelines to use. We are adding ApplicationInsights only.
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddApplicationInsights();

            });
```

While the above approach can be used in a Asp.Net Core application as well, a more common approach would be to combine regular Application monitoring (Requests, Dependencies etc.)
with ILogger capture. 
Install additional package
```
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.6.0-beta3" />
```

Add the following to `ConfigureServices` method. This will enable regular Application Monitoring with default conifguration (ServerTelemetryChannel, LiveMetrics, Request/Dependencies, Correlation etc.)
```
services.AddApplicationInsightsTelemetry("ikeyhere");
```

In this example, the configuration used `ApplicationInsightsLoggerProvider` is the same as used by regular Application Monitoring. There is an exemption to this however. 
The default `TelemetryConfguration` is not quite setup when logging something from Program.cs or Startup.cs itself, so those logs won't have the default configuration. However,
every other logs (e.g. logs from Controllers, Models etc.) would share the configuration with Application Monitoring.

