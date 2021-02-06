using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Net.Http;
using System.Text;
using HttpSQLHelpers;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;

namespace E2ETestAppCore30.Controllers
{    
    [Route("external/calls")]
    public class ExternalCallsController : Controller
    {
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";
        private const string UrlTestWebApiGetCallTemplate = "http://{0}:80/api/values";
        public const string UrlWhichReturns500Format = "http://{0}:80/api/values/999";
        public static string UrlTestWebApiGetCall;
        public static string UrlWhichReturns500;

        /// <summary>
        /// Connection string format.
        /// </summary>         
        public string ConnectionStringFormat = "Server = {0};Initial Catalog=dependencytest;User Id = sa; Password=MSDNm4g4z!n4"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]

        /// <summary>
        /// Connection string to local database.
        /// </summary>         
        public static string ConnectionString;

        /// <summary>
        /// Valid SQL Query. The wait for delay of 6 ms is used to prevent access time of less than 1 ms. SQL is not accurate below 3, so used 6 ms delay.
        /// </summary>         
        private const string ValidSqlQueryToApmDatabase = "WAITFOR DELAY '00:00:00:007';select * from dbo.Messages";

        /// <summary>
        /// Valid SQL Query to get count.
        /// </summary> 
        private const string ValidSqlQueryCountToApmDatabase = "WAITFOR DELAY '00:00:00:007';select count(*) from dbo.Messages";

        /// <summary>
        /// Invalid SQL Query.
        /// </summary> 
        private const string InvalidSqlQueryToApmDatabase = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabasesunknown";

        /// <summary>
        /// Label used to identify the query being executed.
        /// </summary> 
        private const string QueryToExecuteLabel = "Query Executed:";

        private readonly TelemetryConfiguration telemetryConfiguration;

        private string GetQueryValue(string valueKey)
        {
            return Request.Query[valueKey].ToString();
        }

        public ExternalCallsController(IOptions<AppInsightsOptions> options, TelemetryConfiguration telemetryConfiguration)
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                ConnectionString = string.Format(ConnectionStringFormat, options.Value.SqlServerInstance);
            }

            var webApiHostName = options.Value.Webapihostname;
            UrlTestWebApiGetCall = string.Format(UrlTestWebApiGetCallTemplate, webApiHostName);
            UrlWhichReturns500 = string.Format(UrlWhichReturns500Format, webApiHostName);
            this.telemetryConfiguration = telemetryConfiguration;
        }

        // GET external/calls
        [HttpGet]
        public string Get()
        {
            string title = "(No title set)";
            string response = "(No response set)";

            string type = GetQueryValue("type");
            string countStr = GetQueryValue("count");

            bool.TryParse(GetQueryValue("success"), out var success);
            string sqlQueryTouse = success ? ValidSqlQueryToApmDatabase : InvalidSqlQueryToApmDatabase;

            int count;
            if (!int.TryParse(countStr, out count))
            {
                count = 1;
            }

            switch (type)
            {
                case "flush":
                    title = response = "Flushed telemetry channel";
                    telemetryConfiguration.TelemetryChannel.Flush();                    
                    break;
                case "setsqlserverinstance":
                    string sqlServerInstance = GetQueryValue("server");
                    if(!string.IsNullOrEmpty(sqlServerInstance))
                    {
                        ConnectionString = string.Format(ConnectionStringFormat, sqlServerInstance);
                        title = response = "Update SQL Server Instance to: " + sqlServerInstance;
                    }
                    else
                    {
                        title = response = "SQL Server Instance not updated. ";
                    }                    
                    
                    break;
                case "setchannelendpoint":
                    string endPoint = GetQueryValue("endpoint");
                    if (!string.IsNullOrEmpty(endPoint))
                    {
                        telemetryConfiguration.TelemetryChannel.EndpointAddress = string.Format(Program.EndPointAddressFormat, endPoint);
                        title = response = "Update Endpoint to: " + telemetryConfiguration.TelemetryChannel.EndpointAddress;
                    }
                    else
                    {
                        title = response = "Endpoint not updated. ";
                    }

                    break;

                case "http":
                    title = "Made Sync GET HTTP call to bing";
                    MakeHttpGetCallSync(count, UrlTestWebApiGetCall);
                    response = title;
                    break;
                case "httppost":
                    title = "Made Sync POST HTTP call to bing";
                    MakeHttpPostCallSync(count, UrlTestWebApiGetCall);
                    response = title;
                    break;
                case "http500":
                    title = "Made failing (500) Sync GET HTTP call";
                    MakeHttpCallSync500(count, UrlWhichReturns500);
                    response = title;
                    break;
                case "httpexception":
                    title = "Made Sync GET HTTP call without response (DNS issue)";
                    MakeHttpCallSyncException(count, UrlWithNonexistentHostName);
                    response = title;
                    break;
                case "ExecuteReaderAsync":
                    SqlCommandHelper.ExecuteReaderAsync(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "ExecuteScalarAsync":
                    SqlCommandHelper.ExecuteScalarAsync(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "ExecuteReaderStoredProcedureAsync":
                    this.ExecuteReaderStoredProcedureAsync();
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "TestExecuteReaderTwiceWithTasks":
                    SqlCommandHelper.AsyncExecuteReaderInTasks(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "ExecuteNonQueryAsync":
                    SqlCommandHelper.ExecuteNonQueryAsync(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "ExecuteXmlReaderAsync":
                    sqlQueryTouse += " FOR XML AUTO";
                    SqlCommandHelper.ExecuteXmlReaderAsync(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
                    break;
                case "SqlCommandExecuteScalar":
                    sqlQueryTouse = success
                                    ? ValidSqlQueryCountToApmDatabase
                                    : InvalidSqlQueryToApmDatabase;
                    SqlCommandHelper.ExecuteScalar(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse + ConnectionString;
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
        private static string MakeHttpGetCallSync(int count, string target)
        {
            string result = "";

            Uri ourUri = new Uri(target);
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
        private static string MakeHttpPostCallSync(int count, string target)
        {
            string result = "";   
            HttpClient client = new HttpClient();
            var content = ("helloworld");
            result += client.PostAsync(target, new StringContent(content.ToString(), Encoding.UTF8, "application/json")).Result;

            return result;
        }

        /// <summary>
        /// Make sync http calls which return 500
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        private static string MakeHttpCallSync500(int count, string target)
        {
            string result = "";

            Uri ourUri = new Uri(target);
            HttpClient client = new HttpClient();
            for (int i = 0; i < count; ++i)
            {
                result += $"Request {i + 1}:<BR/>";
                var response = client.GetAsync(ourUri).Result;

                result += response.Content.ReadAsStringAsync();
            }

            return result;
        }

        /// <summary>
        /// Make sync http calls which do not have response and throw an Exception.
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        private static string MakeHttpCallSyncException(int count, string target)
        {
            string result = "";

            Uri ourUri = new Uri(target);
            HttpClient client = new HttpClient();
            for (int i = 0; i < count; ++i)
            {
                result += $"Request {i + 1}:<BR/>";
                try
                {
                    result += client.GetAsync(ourUri).Result;
                }
                catch (Exception e)
                {
                    result += "Exception occured (as expected):" + e;
                }
            }

            return result;
        }

        /// <summary>
        /// Executes reader stored procedure.
        /// </summary>        
        private void ExecuteReaderStoredProcedureAsync()
        {
            var storedProcedureName = GetQueryValue("storedProcedureName");
            if (string.IsNullOrEmpty(storedProcedureName))
            {
                throw new ArgumentException("storedProcedureName query string parameter is not defined.");
            }

            SqlCommandHelper.ExecuteReaderAsync(ConnectionString, storedProcedureName, CommandType.StoredProcedure);
        }
    }
}