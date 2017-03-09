using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;

namespace AspxCore.Controllers
{
    [Route("external/calls")]
    public class ExternalCallsController : Controller
    {/// <summary>
     /// Invalid Hostname to trigger exception being thrown
     /// </summary>
        private const string InvalidHostName = "http://www.zzkaodkoakdahdjghejajdnad.com";

        private string GetQueryValue(string valueKey)
        {
            return Request.Query[valueKey].ToString();
        }

        // GET external/calls
        [HttpGet]
        public string Get()
        {
            string title = "(No title set)";
            string response = "(No response set)";

            string type = GetQueryValue("type");
            string countStr = GetQueryValue("count");

            int count;
            if (!int.TryParse(countStr, out count))
            {
                count = 1;
            }

            switch (type)
            {
                case "http":
                    title = "Made Sync GET HTTP call to bing";
                    response = MakeHttpGetCallSync(count, "bing");
                    break;
                case "httppost":
                    title = "Made Sync POST HTTP call to bing";
                    response = MakeHttpPostCallSync(count, "bing");
                    break;
                case "failedhttp":
                    title = "Made failing Sync GET HTTP call to bing";
                    response = MakeHttpCallSyncFailed(count);
                    break;
                default:
                    title = $"Unrecognized request type '{type}'";
                    response = "";
                    break;
            }

            return $"<HTML><BODY>{title}<BR/>{response}</BODY></HTML>";
        }

        /// <summary>
        /// Make sync http GET calls
        /// </summary>        
        /// <param name="count">no of GET calls to be made</param>        
        /// <param name="hostname">the GET call will be made to http://www.hostname.com</param>        
        private static string MakeHttpGetCallSync(int count, string hostname)
        {
            string result = "";

            Uri ourUri = new Uri(string.Format("http://www.{0}.com", hostname));
            HttpClient client = new HttpClient();
            for (int i = 0; i < count; i++)
            {
                result += $"Request {i + 1}:<BR/>{client.GetStringAsync(ourUri).Result}<BR/>";
            }

            return result;
        }

        /// <summary>
        /// Make sync http POST calls
        /// </summary>        
        /// <param name="count">no of POST calls to be made</param>        
        /// <param name="hostname">the POST call will be made to http://www.hostname.com</param>        
        private static string MakeHttpPostCallSync(int count, string hostname)
        {
            string result = "";

            Uri ourUri = new Uri(string.Format("http://www.{0}.com", hostname));
            HttpClient client = new HttpClient();
            HttpContent content = new StringContent("thing1=hello&thing2=world", Encoding.ASCII);
            for (int i = 0; i < count; i++)
            {
                result += $"Request {i + 1}:<BR/>{client.PostAsync(ourUri, content).Result}<BR/>";
            }

            return result;
        }

        /// <summary>
        /// Make sync http calls which fails
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        private static string MakeHttpCallSyncFailed(int count)
        {
            string result = "";

            Uri ourUri = new Uri(InvalidHostName);
            HttpClient client = new HttpClient();
            for (int i = 0; i < count; ++i)
            {
                result += $"Request {i + 1}:<BR/>";
                try
                {
                    result += client.GetStringAsync(ourUri).Result;
                }
                catch (Exception e)
                {
                    result += "Exception occured (as expected):" + e;
                }
            }

            return result;
        }
    }
}
