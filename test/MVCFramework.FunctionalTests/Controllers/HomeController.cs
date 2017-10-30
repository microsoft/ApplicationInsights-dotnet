using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;

namespace MVCFramework45.FunctionalTests.Controllers
{
    public class HomeController : Controller
    {
        private TelemetryClient telemetryClient;

        public HomeController(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Exception()
        {
            throw new InvalidOperationException("Do not call the method called Exception");
        }

        public IActionResult About(int index)
        {
            ViewBag.Message = "Your application description page # " + index;

            return View();
        }

        public IActionResult Contact()
        {
            this.telemetryClient.TrackEvent("GetContact");
            this.telemetryClient.TrackMetric("ContactFile", 1);
            this.telemetryClient.TrackTrace("Fetched contact details.", SeverityLevel.Information);
            return View();
        }

        public IActionResult Dependency()
        {
            this.telemetryClient.TrackDependency("MyDependency", "MyCommand", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1), success: true);
            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
