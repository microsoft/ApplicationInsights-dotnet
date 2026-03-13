namespace WebApp.AspNetCore.Controllers
{
    using System;
    using System.Diagnostics;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    using WebApp.AspNetCore.Models;

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly TelemetryConfiguration _telemetryConfiguration;

        public HomeController(ILogger<HomeController> logger, TelemetryClient telemetryClient, TelemetryConfiguration telemetryConfiguration)
        {
            this._logger = logger;
            this._telemetryClient = telemetryClient;
            telemetryClient.Context.User.Id = "TestUserId";

            // In a real app, you wouldn't need the TelemetryConfiguration here.
            // This is included in this sample because it allows you to debug and verify that the configuration at runtime matches the expected configuration.
            this._telemetryConfiguration = telemetryConfiguration;
        }

        public IActionResult Index()
        {
            this._telemetryClient.TrackEvent(eventName: "Hello World!");
            this._telemetryClient.TrackTrace(message: "This is a trace message.");
            this._telemetryClient.TrackException(exception: new Exception("This is a test exception."));
            this._telemetryClient.TrackDependency(dependencyTypeName: "HTTP", target: "www.example.com", dependencyName: "GET /api/test", data: null, startTime: DateTimeOffset.Now, duration: TimeSpan.FromMilliseconds(100), resultCode: "200", success: true);
            this._telemetryClient.TrackRequest("Test Request", DateTimeOffset.Now, TimeSpan.FromMilliseconds(123), "200", true);

            _logger.LogInformation("Hello from HomeController.Index!");
            _logger.LogError(new Exception("This is a test exception logged with ILogger."), "This is a test exception logged with ILogger.");

            using (var operation = this._telemetryClient.StartOperation<Microsoft.ApplicationInsights.DataContracts.RequestTelemetry>("TestOperation"))
            {
                operation.Telemetry.Properties["CustomProperty"] = "CustomValue";
                // Simulate some work
                System.Threading.Thread.Sleep(100);
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
