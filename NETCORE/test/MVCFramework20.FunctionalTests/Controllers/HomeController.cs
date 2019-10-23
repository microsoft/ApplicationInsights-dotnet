using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVCFramework20.FunctionalTests.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace MVCFramework20.FunctionalTests.Controllers
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
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Exception()
        {
            throw new InvalidOperationException("Do not call the method called Exception");
        }

        public IActionResult Dependency()
        {
            this.telemetryClient.TrackDependency("MyDependency", "MyCommand", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1), success: true);
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
