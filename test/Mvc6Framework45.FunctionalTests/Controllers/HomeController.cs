using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.PlatformAbstractions;

namespace Mvc6Framework45.FunctionalTests.Controllers
{
    public class HomeController : Controller
    {
        private TelemetryClient telemetryClient;
        private IApplicationEnvironment applicationEnvironment;

        public HomeController(TelemetryClient telemetryClient, IApplicationEnvironment applicationEnvironment)
        {
            this.telemetryClient = telemetryClient;
            this.applicationEnvironment = applicationEnvironment;
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

        public async Task<IActionResult> Contact()
        {
            ViewBag.Message = await this.GetContactDetails();

            return View();
        }

        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }

        private async Task<string> GetContactDetails()
        {
            var contactFilePath = Path.Combine(this.applicationEnvironment.ApplicationBasePath, "contact.txt");
            this.telemetryClient.TrackEvent("GetContact");

            using (var reader = System.IO.File.OpenText(contactFilePath))
            {
                var contactDetails = await reader.ReadToEndAsync();
                this.telemetryClient.TrackMetric("ContactFile", 1);
                this.telemetryClient.TrackTrace("Fetched contact details.", SeverityLevel.Information);
                return contactDetails;
            }
        }
    }
}