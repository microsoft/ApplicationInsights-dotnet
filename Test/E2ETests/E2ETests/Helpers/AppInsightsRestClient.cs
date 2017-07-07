using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace E2ETests.Helpers
{
    internal class AppInsightsRestClient
    {
        private string AiAppId;
        private string AiAppKey;
        private const string URL = "https://api.applicationinsights.io/beta/apps/{0}/events/{1}?timespan={2}";

        public AppInsightsRestClient(string appid, string appKey)
        {
            this.AiAppId = appid;
            this.AiAppKey = appKey;
        }

        public ArrayList GetRequests(string timespan)
        {
            ArrayList requests = new ArrayList();
            var jsonResults = GetResultsAsJson("requests", timespan);
            dynamic jsonDe = JsonConvert.DeserializeObject(jsonResults);

            foreach (var request in jsonDe.value)
            {
                var req = new Request();
                req.id = request.id;
                req.timestamp = request.timestamp;
                req.requestName = request.request.name;
                req.resultCode = request.request.resultCode;
                req.source = request.request.source;
                req.operationid = request.operation.id;

                requests.Add(req);
            }

            return requests;
        }

        public ArrayList GetDependencies(string timespan)
        {
            ArrayList dependencies = new ArrayList();
            var jsonResults = GetResultsAsJson("dependencies", timespan);
            dynamic jsonDe = JsonConvert.DeserializeObject(jsonResults);

            foreach (var dependency in jsonDe.value)
            {
                var dep = new Dependency();
                dep.timestamp = dependency.timestamp;
                dep.target = dependency.dependency.target;
                dep.data = dependency.dependency.data;
                dep.success = dependency.dependency.success;
                dep.type = dependency.dependency.type;
                dep.name = dependency.dependency.name;
                dep.id = dependency.dependency.id;
                dep.operationparentid = dependency.operation.parentId;
                dependencies.Add(dep);
            }
            return dependencies;
        }

        private string GetResultsAsJson(string type, string timespan)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", AiAppKey);
            var req = string.Format(URL, AiAppId, type, timespan);
            HttpResponseMessage response = client.GetAsync(req).Result;
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return response.ReasonPhrase;
            }
        }
    }
}
