using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApi20.FunctionalTests.Controllers
{
    [Route("api/[controller]")]
    public class DependencyController : Controller
    {
        // GET api/values
        [HttpGet]
        public async Task Get()
        {
            using (var hc = new HttpClient())
            {
                await hc.GetAsync("https://www.microsoft.com").ContinueWith(t => { }); // ignore all errors
            }
        }
    }
}
