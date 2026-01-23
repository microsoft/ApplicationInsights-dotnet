# TelemetryClient.TrackPageView() Removed

**Category:** Breaking Change  
**Applies to:** TelemetryClient API  
**Migration Effort:** Simple  
**Related:** [TrackRequest-behavior-changed.md](TrackRequest-behavior-changed.md), [TrackEvent-behavior-changed.md](TrackEvent-behavior-changed.md)

## Change Summary

The `TrackPageView()` method has been completely removed from `TelemetryClient` in 3.x. Page view tracking is primarily a client-side (browser JavaScript) concern. For server-side scenarios, use `TrackEvent()` or `TrackRequest()` instead.

## API Comparison

### 2.x API

```csharp
// Source: ApplicationInsights-dotnet-2x/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs:602-621
public void TrackPageView(string name)
{
    this.Track(new PageViewTelemetry(name));
}

public void TrackPageView(PageViewTelemetry telemetry)
{
    if (telemetry == null)
    {
        telemetry = new PageViewTelemetry();
    }
    
    this.Track(telemetry);
}
```

### 3.x API

```csharp
// Source: ApplicationInsights-dotnet/BASE/src/Microsoft.ApplicationInsights/TelemetryClient.cs
// REMOVED: TrackPageView() methods do not exist

// Available alternatives:
public void TrackEvent(string eventName, IDictionary<string, string> properties = null);
public void TrackRequest(string name, DateTimeOffset startTime, TimeSpan duration, string responseCode, bool success);
```

## Why It Changed

| Reason | Description |
|--------|-------------|
| **Client-Side Tracking** | Page views are primarily tracked by browser JavaScript SDK, not server-side |
| **Redundant with Requests** | Server-side page rendering is already captured by request tracking |
| **Semantic Clarity** | Server-side "page views" are better represented as events or requests |
| **OpenTelemetry Alignment** | OpenTelemetry doesn't have a "page view" concept - uses events and spans |

## Migration Strategies

### Option 1: Client-Side JavaScript (Recommended)

**When to use:** Single Page Applications (SPA), MVC/Razor Pages with client-side routing.

**2.x:**
```csharp
// Server-side tracking (anti-pattern)
public class HomeController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult Index()
    {
        telemetryClient.TrackPageView("HomePage");
        return View();
    }
}
```

**3.x:**
```html
<!-- Client-side tracking (recommended) -->
<!-- _Layout.cshtml or index.html -->
<script type="text/javascript">
    var sdkInstance="appInsightsSDK";window[sdkInstance]="appInsights";
    var aiName=window[sdkInstance],aisdk=window[aiName]||function(e){
        // ... Application Insights JavaScript SDK snippet ...
    }();
    
    aisdk.queue.push(function() {
        aisdk.trackPageView({name: "HomePage"});
    });
</script>

<!-- Or use NPM package -->
<script>
    import { ApplicationInsights } from '@microsoft/applicationinsights-web';
    
    const appInsights = new ApplicationInsights({ config: {
        connectionString: 'InstrumentationKey=...'
    }});
    appInsights.loadAppInsights();
    appInsights.trackPageView({name: 'HomePage'});
</script>
```

### Option 2: Track as Event (Server-Side)

**When to use:** Need server-side tracking for analytics (non-page scenarios).

**2.x:**
```csharp
public class ReportController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult ViewReport(int reportId)
    {
        telemetryClient.TrackPageView($"Report_{reportId}");
        return View();
    }
}
```

**3.x:**
```csharp
public class ReportController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult ViewReport(int reportId)
    {
        telemetryClient.TrackEvent("ReportViewed", new Dictionary<string, string>
        {
            ["ReportId"] = reportId.ToString(),
            ["PageName"] = $"Report_{reportId}",
            ["UserId"] = User.Identity.Name
        });
        
        return View();
    }
}
```

### Option 3: Use Automatic Request Tracking

**When to use:** Server-rendered pages where request tracking is sufficient.

**2.x:**
```csharp
public class ProductController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult Details(int id)
    {
        // Manually track page view
        telemetryClient.TrackPageView($"Product_{id}");
        
        var product = GetProduct(id);
        return View(product);
    }
}
```

**3.x:**
```csharp
public class ProductController : Controller
{
    // No manual tracking needed - ASP.NET Core instrumentation automatically tracks requests
    public IActionResult Details(int id)
    {
        // Optionally add custom dimensions to current Activity
        var activity = Activity.Current;
        activity?.SetTag("product.id", id);
        activity?.SetTag("page.name", $"Product_{id}");
        
        var product = GetProduct(id);
        return View(product);
    }
}
```

## Common Scenarios

### Scenario 1: MVC Application with Server-Rendered Views

**2.x:**
```csharp
public class AccountController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult Login()
    {
        telemetryClient.TrackPageView("LoginPage");
        return View();
    }
    
    public IActionResult Register()
    {
        telemetryClient.TrackPageView("RegisterPage");
        return View();
    }
}
```

**3.x Option A (Rely on automatic request tracking):**
```csharp
public class AccountController : Controller
{
    // Automatic request tracking captures route: GET /Account/Login
    public IActionResult Login()
    {
        // Optionally enrich with custom data
        Activity.Current?.SetTag("page.type", "Authentication");
        return View();
    }
    
    public IActionResult Register()
    {
        Activity.Current?.SetTag("page.type", "Authentication");
        return View();
    }
}
```

**3.x Option B (Explicit event tracking):**
```csharp
public class AccountController : Controller
{
    private readonly TelemetryClient telemetryClient;
    
    public IActionResult Login()
    {
        telemetryClient.TrackEvent("PageView", new Dictionary<string, string>
        {
            ["PageName"] = "LoginPage",
            ["PageType"] = "Authentication"
        });
        return View();
    }
    
    public IActionResult Register()
    {
        telemetryClient.TrackEvent("PageView", new Dictionary<string, string>
        {
            ["PageName"] = "RegisterPage",
            ["PageType"] = "Authentication"
        });
        return View();
    }
}
```

### Scenario 2: Blazor Server Application

**2.x:**
```csharp
@page "/counter"
@inject TelemetryClient TelemetryClient

<h1>Counter</h1>

@code {
    protected override void OnInitialized()
    {
        TelemetryClient.TrackPageView("CounterPage");
    }
}
```

**3.x:**
```csharp
@page "/counter"
@inject TelemetryClient TelemetryClient

<h1>Counter</h1>

@code {
    protected override void OnInitialized()
    {
        // Track as event with page semantics
        TelemetryClient.TrackEvent("PageView", new Dictionary<string, string>
        {
            ["PageName"] = "CounterPage",
            ["PageRoute"] = "/counter"
        });
    }
}
```

### Scenario 3: Razor Pages

**2.x:**
```csharp
public class IndexModel : PageModel
{
    private readonly TelemetryClient telemetryClient;
    
    public void OnGet()
    {
        telemetryClient.TrackPageView("HomePage");
    }
}
```

**3.x:**
```csharp
public class IndexModel : PageModel
{
    // Automatic request tracking via ASP.NET Core instrumentation
    // No manual tracking needed
    
    public void OnGet()
    {
        // Optionally add context to current activity
        Activity.Current?.SetTag("page.name", "HomePage");
        Activity.Current?.DisplayName = "GET /Index";
    }
}
```

### Scenario 4: API Endpoint Analytics

**2.x:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly TelemetryClient telemetryClient;
    
    [HttpPost("pageview")]
    public IActionResult LogPageView([FromBody] PageViewData data)
    {
        telemetryClient.TrackPageView(data.PageName);
        return Ok();
    }
}
```

**3.x:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly TelemetryClient telemetryClient;
    
    [HttpPost("pageview")]
    public IActionResult LogPageView([FromBody] PageViewData data)
    {
        telemetryClient.TrackEvent("PageView", new Dictionary<string, string>
        {
            ["page.name"] = data.PageName,
            ["page.url"] = data.Url,
            ["referrer"] = data.Referrer
        });
        return Ok();
    }
}
```

## Azure Monitor Queries

### Finding Page View Data

**2.x Query:**
```kusto
pageViews
| where name == "HomePage"
| summarize count() by bin(timestamp, 1h)
```

**3.x Query (if tracked as events):**
```kusto
customEvents
| where name == "PageView"
| where customDimensions.PageName == "HomePage"
| summarize count() by bin(timestamp, 1h)
```

**3.x Query (using request data):**
```kusto
requests
| where url endswith "/Home/Index"
| where success == true
| summarize count() by bin(timestamp, 1h)
```

## JavaScript SDK for Client-Side Tracking

### Installation

**NPM:**
```bash
npm install @microsoft/applicationinsights-web
```

**Configuration:**
```typescript
import { ApplicationInsights } from '@microsoft/applicationinsights-web';

const appInsights = new ApplicationInsights({
    config: {
        connectionString: 'InstrumentationKey=...;IngestionEndpoint=https://...'
    }
});

appInsights.loadAppInsights();

// Automatic page view tracking
appInsights.trackPageView();

// Manual page view tracking
appInsights.trackPageView({
    name: 'ProductDetails',
    uri: window.location.href,
    properties: {
        productId: '12345',
        category: 'Electronics'
    }
});
```

### React Integration

```typescript
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';
import { createBrowserHistory } from 'history';

const browserHistory = createBrowserHistory();
const reactPlugin = new ReactPlugin();

const appInsights = new ApplicationInsights({
    config: {
        connectionString: 'InstrumentationKey=...',
        extensions: [reactPlugin],
        extensionConfig: {
            [reactPlugin.identifier]: { history: browserHistory }
        }
    }
});

appInsights.loadAppInsights();

// Automatic page view tracking on route changes
```

## Migration Checklist

- [ ] Identify all `TrackPageView()` calls in server-side code
- [ ] For each usage, determine the appropriate migration:
  - [ ] Client-side tracking with JavaScript SDK (recommended for actual page views)
  - [ ] `TrackEvent()` for server-side analytics
  - [ ] Rely on automatic request tracking
  - [ ] Enrich existing requests with Activity tags
- [ ] If using client-side tracking:
  - [ ] Install Application Insights JavaScript SDK
  - [ ] Configure with ConnectionString
  - [ ] Add tracking code to layout/app shell
- [ ] Update Azure Monitor queries:
  - [ ] Change `pageViews` table to `customEvents` or `requests`
  - [ ] Update dashboard queries and alerts
- [ ] Update analytics reports and KPIs
- [ ] Remove `PageViewTelemetry` class references

## See Also

- [TrackEvent-behavior-changed.md](TrackEvent-behavior-changed.md) - TrackEvent migration
- [TrackRequest-behavior-changed.md](TrackRequest-behavior-changed.md) - Request tracking changes
- [Application Insights JavaScript SDK](https://docs.microsoft.com/en-us/azure/azure-monitor/app/javascript) - Client-side page view tracking
