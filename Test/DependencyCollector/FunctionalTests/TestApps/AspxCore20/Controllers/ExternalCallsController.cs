using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text;
using FW45Shared;

namespace AspxCore20.Controllers
{
    [Route("external/calls")]
    public class ExternalCallsController : Controller
    {       /// <summary>
         /// Invalid Hostname to trigger exception being thrown
         /// </summary>
        private const string InvalidHostName = "http://www.zzkaodkoakdahdjghejajdnad.com";

        /// <summary>
        /// Connection string to APM Development database.
        /// </summary> 
        private const string ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=RDDTestDatabase;Integrated Security=True";

        /// <summary>
        /// Valid SQL Query. The wait for delay of 6 ms is used to prevent access time of less than 1 ms. SQL is not accurate below 3, so used 6 ms delay.
        /// </summary> 
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        private const string ValidSqlQueryToApmDatabase = "WAITFOR DELAY '00:00:00:006'; select * from dbo.Messages";

        /// <summary>
        /// Valid SQL Query to get count.
        /// </summary> 
        private const string ValidSqlQueryCountToApmDatabase = "WAITFOR DELAY '00:00:00:006'; SELECT count(*) FROM dbo.Messages";

        /// <summary>
        /// Invalid SQL Query.
        /// </summary> 
        private const string InvalidSqlQueryToApmDatabase = "SELECT TOP 2 * FROM apm.[Database1212121]";

        /// <summary>
        /// Label used to identify the query being executed.
        /// </summary> 
        private const string QueryToExecuteLabel = "Query Executed:";

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

            bool.TryParse(GetQueryValue("success"), out var success);
            string sqlQueryTouse = success ? ValidSqlQueryToApmDatabase : InvalidSqlQueryToApmDatabase;

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
                case "ExecuteReaderAsync":
                    SqlCommandHelper.ExecuteReaderAsync(ConnectionString, sqlQueryTouse);
                    response = QueryToExecuteLabel + sqlQueryTouse;
                    break;
                case "ExecuteScalarAsync":
                    SqlCommandHelper.ExecuteScalarAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteReaderStoredProcedureAsync":
                    this.ExecuteReaderStoredProcedureAsync();
                    break;
                case "TestExecuteReaderTwiceWithTasks":
                    SqlCommandHelper.AsyncExecuteReaderInTasks(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteNonQueryAsync":
                    SqlCommandHelper.ExecuteNonQueryAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteXmlReaderAsync":
                    sqlQueryTouse += " FOR XML AUTO";
                    SqlCommandHelper.ExecuteXmlReaderAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "SqlCommandExecuteScalar":
                    sqlQueryTouse = success
                                    ? ValidSqlQueryCountToApmDatabase
                                    : InvalidSqlQueryToApmDatabase;
                    SqlCommandHelper.ExecuteScalar(ConnectionString, sqlQueryTouse);
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

            Uri ourUri = new Uri(string.Format("https://www.{0}.com", hostname));
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

            Uri ourUri = new Uri(string.Format("https://www.{0}.com", hostname));
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