# HTTP Client/Server Instrumentation

## Package

```bash
dotnet add package OpenTelemetry.Instrumentation.Http
```

**Note:** The SDK already registers both HTTP client (`AddHttpClientInstrumentation`) and HTTP server (`AddAspNetCoreInstrumentation`) instrumentation. **Do not call these again.** Use `services.Configure<TOptions>` to customize options without duplicating instrumentation.

## Configuring HTTP Client Options

```csharp
using OpenTelemetry.Instrumentation.Http;
using System.Net.Http;

builder.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
    options.FilterHttpRequestMessage = (httpRequestMessage) =>
    {
        // Only collect telemetry about HTTP GET requests
        return httpRequestMessage.Method == HttpMethod.Get;
    };
    options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
    {
        if (httpRequestMessage.Headers.TryGetValues("x-request-id", out var values))
        {
            activity.SetTag("http.request.header.x-request-id", values.FirstOrDefault());
        }
    };
});
```

### HTTP Client Options

| Option | Default | Description |
|---|---|---|
| `RecordException` | `false` | Record exception details as span events |
| `FilterHttpRequestMessage` | `null` | `Func<HttpRequestMessage, bool>` — return `false` to suppress a span |
| `EnrichWithHttpRequestMessage` | `null` | `Action<Activity, HttpRequestMessage>` to add custom tags from the request |
| `EnrichWithHttpResponseMessage` | `null` | `Action<Activity, HttpResponseMessage>` to add custom tags from the response |
| `EnrichWithException` | `null` | `Action<Activity, Exception>` to add custom tags on failure |

## Configuring HTTP Server Options (ASP.NET Core)

```csharp
using OpenTelemetry.Instrumentation.AspNetCore;

builder.Services.Configure<AspNetCoreTraceInstrumentationOptions>(options =>
{
    options.RecordException = true;
    options.Filter = (httpContext) =>
    {
        // Skip health check endpoints
        return !httpContext.Request.Path.StartsWithSegments("/health");
    };
    options.EnrichWithHttpRequest = (activity, httpRequest) =>
    {
        activity.SetTag("http.request.header.tenant",
            httpRequest.Headers["X-Tenant-Id"].FirstOrDefault());
    };
});
```

### HTTP Server Options

| Option | Default | Description |
|---|---|---|
| `RecordException` | `false` | Record exception details as span events |
| `Filter` | `null` | `Func<HttpContext, bool>` — return `false` to suppress a span |
| `EnrichWithHttpRequest` | `null` | `Action<Activity, HttpRequest>` to add custom tags from the request |
| `EnrichWithHttpResponse` | `null` | `Action<Activity, HttpResponse>` to add custom tags from the response |
| `EnrichWithException` | `null` | `Action<Activity, Exception>` to add custom tags on failure |

## Non-DI Usage (Console / Library)

For non-DI console apps or libraries using `TelemetryConfiguration` directly (base `Microsoft.ApplicationInsights` without a host), you must install the package and explicitly add HTTP client instrumentation to capture outgoing HTTP calls. Classic ASP.NET apps already include HTTP client instrumentation by default.

```bash
dotnet add package OpenTelemetry.Instrumentation.Http
```

```csharp
var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
{
    otel.WithTracing(t => t.AddHttpClientInstrumentation());

    otel.Services.Configure<HttpClientTraceInstrumentationOptions>(options =>
    {
        options.RecordException = true;
    });
});
```

Note: `AspNetCoreTraceInstrumentationOptions` does not apply to non-DI scenarios (Console/Classic ASP.NET don't use the ASP.NET Core server pipeline).

## Notes

- The SDK already filters Azure SDK internal HTTP calls to avoid duplicate spans
- HTTP metrics (`System.Net.Http` and `Microsoft.AspNetCore.Hosting` meters) are registered by the SDK automatically — no additional setup needed
