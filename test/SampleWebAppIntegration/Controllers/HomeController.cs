using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;

namespace SampleWebAppIntegration.Controllers
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

        public IActionResult About()
        {
            ViewBag.Message = "Your application description page.";

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
            var applicationEnvironment = (IApplicationEnvironment)this.Resolver.GetService(typeof(IApplicationEnvironment));
            var contactFilePath = Path.Combine(applicationEnvironment.ApplicationBasePath, "contact.txt");
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