# OpenTelemetry Pipeline

## Overview

The OpenTelemetry pipeline is the data flow path for telemetry signals (traces, metrics, logs) from your application to observability backends. Understanding this pipeline is essential for Azure Monitor integration.

## Pipeline Components

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│  Instrumentation │ ──▶ │   Processors    │ ──▶ │    Exporters    │
│  (Sources)       │     │   (Transform)   │     │   (Backends)    │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

### 1. Instrumentation (Sources)
- **ActivitySource** — Creates spans/traces
- **Meter** — Creates metrics
- **ILogger** — Creates logs (via OpenTelemetry.Extensions.Logging)

### 2. Processors (Transform)
- **BaseProcessor\<Activity\>** — Enrich or filter spans
- **BaseProcessor\<LogRecord\>** — Enrich or filter logs
- Run in pipeline order before export

### 3. Exporters (Backends)
- **Azure Monitor Exporter** — Sends to Application Insights
- **OTLP Exporter** — Sends to any OTLP-compatible backend
- **Console Exporter** — Debug output

## Key Differences from Application Insights 2.x

| 2.x Concept | 3.x Equivalent |
|---|---|
| TelemetryClient | ActivitySource / Meter / ILogger |
| ITelemetryInitializer | BaseProcessor\<Activity\>.OnStart |
| ITelemetryProcessor | BaseProcessor\<Activity\>.OnEnd |
| TelemetryChannel | Exporter |
| TelemetryConfiguration | OpenTelemetryBuilder |
