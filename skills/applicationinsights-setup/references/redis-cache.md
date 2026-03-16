# Redis Instrumentation (StackExchange.Redis)

## Package

```bash
dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis
```

## Setup

```csharp
using OpenTelemetry.Instrumentation.StackExchangeRedis;

// Register IConnectionMultiplexer in DI
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

// Add Redis instrumentation
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddRedisInstrumentation());
```

## With Customization

```csharp
builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
    tracing.AddRedisInstrumentation(options =>
    {
        options.SetVerboseDatabaseStatements = true;
    }));
```

## Options

| Option | Default | Description |
|---|---|---|
| `SetVerboseDatabaseStatements` | `false` | Include full Redis command (e.g., `GET mykey`) |
| `EnrichActivityWithTimingEvents` | `true` | Add timing events (enqueue, sent, response) |
| `Enrich` | `null` | `Action<Activity, IProfiledCommand>` for custom tags |
| `FlushInterval` | 1 second | How often to flush profiling sessions |

## Non-DI Usage (Console / Classic ASP.NET)

For apps using `TelemetryConfiguration` directly, pass the connection explicitly:

```csharp
var connection = ConnectionMultiplexer.Connect("localhost:6379");

var config = TelemetryConfiguration.CreateDefault();
config.ConnectionString = "InstrumentationKey=...;IngestionEndpoint=...";
config.ConfigureOpenTelemetryBuilder(otel =>
    otel.WithTracing(t => t.AddRedisInstrumentation(connection)));
```

## Notes

- Hooks into StackExchange.Redis profiling
- When using DI registration, all `IConnectionMultiplexer` instances are auto-discovered
- Without DI, you must pass the `IConnectionMultiplexer` explicitly — there's no auto-discovery
