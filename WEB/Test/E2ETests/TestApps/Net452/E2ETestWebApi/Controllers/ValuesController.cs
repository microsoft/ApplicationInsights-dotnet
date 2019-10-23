using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.Win32;

namespace E2ETestWebApi.Controllers
{
    public class ValuesController : ApiController
    {
        private static readonly TelemetryClient telemetryClient = new TelemetryClient();

        private static readonly AssemblyFileVersionAttribute objectAssemblyFileVer =
            typeof(HttpApplication)
                .GetTypeInfo()
                .Assembly
                .GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                .Cast<AssemblyFileVersionAttribute>()
                .FirstOrDefault();

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            if(id == 999)
            {
                throw new Exception("999 is exception!");
            }
            return "value";
        }

        // POST api/values
        public Task<HttpResponseMessage> Post([FromBody]string value)
        {
            var response = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.NoContent
            };

            response.Headers.Add("OnExecuteRequestStep", (typeof(HttpApplication).GetMethod("OnExecuteRequestStep") != null).ToString());
            response.Headers.Add(".NetRelease", Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full", "Release", null)?.ToString());
            response.Headers.Add("AspNetAssemblyVersion", objectAssemblyFileVer.Version);

            var restoredActivity = Activity.Current;
            if (restoredActivity != null)
            {
                response.Headers.Add("RestoredActivityId", restoredActivity.Id);
            }

            response.Headers.Add("BodyLength", value?.Length.ToString());
            return Task.Run(async () =>
            {
                using (telemetryClient.StartOperation<DependencyTelemetry>("test"))
                {
                    await Task.Delay(10);
                    return response;
                }
            });
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
