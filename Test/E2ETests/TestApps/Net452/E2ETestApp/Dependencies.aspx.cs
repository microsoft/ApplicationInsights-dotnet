using FW40Shared;
using FW45Shared;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace E2ETestApp
{
    public partial class Dependencies : System.Web.UI.Page
    {
        public const string LocalDbConnectionString = @"Server =sql-server;User Id = sa; Password=MSDNm4g4z!n4";
        public const string InvalidAccountConnectionString = @"Server =sql-server;User Id = sa; Password=thisiswrong";
        public const string InvalidServerConnectionString = @"Server =sql-server-dontexist;User Id = sa; Password=MSDNm4g4z!n4";
        private string SqlQuerySuccess = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabases";
        private string SqlQueryError = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabasesunknown";
        private string SqlStoredProcedureName = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabases";

        private const string UrlTestWebApiGetCallTemplate = "http://{0}:80/api/values";
        private const string UrlGoogle = "http://google.com";
        public const string UrlWhichThrowException = "http://e2etestwebapi:80/api/values/999";
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";

        protected void Page_Load(object sender, EventArgs e)
        {
            var type = Request.QueryString["type"];
            var success = true;
            bool.TryParse(Request.QueryString["success"], out success);
            this.lblRequestedAction.Text = "Requested Action:" + type;

            var webApiHostName = Microsoft.Azure.CloudConfigurationManager.GetSetting("webapihostname");
            string UrlTestWebApiGetCall = string.Format(UrlTestWebApiGetCallTemplate, webApiHostName);

            try
            {
                switch (type)
                {
                    case "etw":
                        new Thread(() =>
                        {
                            Thread.CurrentThread.IsBackground = true;
                            EtwEventSessionRdd EtwSession = new EtwEventSessionRdd();
                            EtwSession.Start();
                            Console.WriteLine("Hello, world");
                        }).Start();
                        HttpHelper40.MakeHttpCallSync(UrlTestWebApiGetCall);
                        break;
                    case "httpsyncgoogle":
                        HttpHelper40.MakeHttpCallSync(UrlGoogle);
                        break;
                    case "httpsync":
                        HttpHelper40.MakeHttpCallSync(UrlTestWebApiGetCall);
                        break;
                    case "httpasynchttpclient":
                        HttpHelper40.MakeHttpCallUsingHttpClient(UrlTestWebApiGetCall);
                        break;
                    case "httppost":
                        HttpHelper40.MakeHttpPostCallSync(UrlTestWebApiGetCall);
                        break;
                    case "httpfailedwithexception":
                        HttpHelper40.MakeHttpCallUsingHttpClient(UrlWhichThrowException);
                        break;
                    case "httpfailedwithinvaliddns":
                        HttpHelper40.MakeHttpCallUsingHttpClient(UrlWithNonexistentHostName);
                        break;
                    case "httpasync1":
                        HttpHelper40.MakeHttpCallAsync1(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync1":
                        HttpHelper40.MakeHttpCallAsync1(UrlWhichThrowException);
                        break;
                    case "httpasync2":
                        HttpHelper40.MakeHttpCallAsync2(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync2":
                        HttpHelper40.MakeHttpCallAsync2(UrlWhichThrowException);
                        break;
                    case "httpasync3":
                        HttpHelper40.MakeHttpCallAsync3(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync3":
                        HttpHelper40.MakeHttpCallAsync3(UrlWhichThrowException);
                        break;
                    case "httpasync4":
                        HttpHelper40.MakeHttpCallAsync4(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync4":
                        HttpHelper40.MakeHttpCallAsync4(UrlWhichThrowException);
                        break;
                    case "httpasyncawait1":
                        HttpHelper45.MakeHttpCallAsyncAwait1(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasyncawait1":
                        HttpHelper45.MakeHttpCallAsyncAwait1(UrlWhichThrowException);
                        break;
                    case "azuresdkblob":
                        string containerName = "rddtest";
                        string blobName = "testblob";
                        try
                        {
                            containerName = Request.QueryString["containerName"];
                            blobName = Request.QueryString["blobName"];
                        }
                        catch (Exception) { }
                        HttpHelper40.MakeAzureCallToWriteToBlobWithSdk(containerName, blobName);
                        HttpHelper40.MakeAzureCallToReadBlobWithSdk(containerName, blobName);
                        break;
                    case "azuresdkqueue":
                        HttpHelper40.MakeAzureCallToWriteQueueWithSdk();
                        break;
                    case "azuresdktable":
                        string tableName = "people";
                        try
                        {
                            tableName = Request.QueryString["tablename"];
                        }
                        catch (Exception) { }
                        HttpHelper40.MakeAzureCallToWriteTableWithSdk(tableName);
                        HttpHelper40.MakeAzureCallToReadTableWithSdk(tableName);
                        break;
                    case "ExecuteReaderAsync":
                        SqlCommandHelper.ExecuteReaderAsync(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "ExecuteScalarAsync":
                        SqlCommandHelper.ExecuteScalarAsync(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "ExecuteReaderStoredProcedureAsync":
                        SqlCommandHelper.ExecuteReaderAsync(LocalDbConnectionString, SqlStoredProcedureName, CommandType.StoredProcedure);
                        break;
                    case "TestExecuteReaderTwice":
                        SqlCommandHelper.TestExecuteReaderTwice(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "BeginExecuteReader0":
                        SqlCommandHelper.BeginExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 0);
                        break;
                    case "BeginExecuteReader1":
                        SqlCommandHelper.BeginExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 1);
                        break;
                    case "BeginExecuteReader2":
                        SqlCommandHelper.BeginExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 2);
                        break;
                    case "BeginExecuteReader3":
                        SqlCommandHelper.BeginExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 3);
                        break;
                    case "TestExecuteReaderTwiceInSequence":
                        SqlCommandHelper.TestExecuteReaderTwiceInSequence(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "TestExecuteReaderTwiceWithTasks":
                        SqlCommandHelper.AsyncExecuteReaderInTasks(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "ExecuteNonQueryAsync":
                        SqlCommandHelper.ExecuteNonQueryAsync(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "BeginExecuteNonQuery0":
                        SqlCommandHelper.BeginExecuteNonQuery(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 0);
                        break;
                    case "BeginExecuteNonQuery2":
                        SqlCommandHelper.BeginExecuteNonQuery(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 2);
                        break;
                    case "ExecuteXmlReaderAsync":
                        SqlQuerySuccess += " FOR XML AUTO";
                        SqlCommandHelper.ExecuteXmlReaderAsync(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "BeginExecuteXmlReader":
                        SqlCommandHelper.BeginExecuteXmlReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "SqlCommandExecuteScalar":
                        SqlCommandHelper.ExecuteScalar(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "SqlCommandExecuteNonQuery":
                        SqlCommandHelper.ExecuteNonQuery(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "SqlCommandExecuteReader0":
                        SqlCommandHelper.ExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 0);
                        break;
                    case "SqlCommandExecuteReader1":
                        SqlCommandHelper.ExecuteReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError, 1);
                        break;
                    case "SqlCommandExecuteXmlReader":
                        SqlQuerySuccess += " FOR XML AUTO";
                        SqlCommandHelper.ExecuteXmlReader(LocalDbConnectionString, (success == true)
                                        ? SqlQuerySuccess
                                        : SqlQueryError);
                        break;
                    case "SqlConnectionOpen":
                        SqlCommandHelper.OpenConnection(LocalDbConnectionString);
                        break;
                    case "SqlConnectionOpenFailedInvalidAccount":
                        SqlCommandHelper.OpenConnection(InvalidAccountConnectionString);
                        break;
                    case "SqlConnectionOpenFailedInvalidServer":
                        SqlCommandHelper.OpenConnection(InvalidServerConnectionString);
                        break;
                    case "SqlConnectionOpenAsync":
                        SqlCommandHelper.OpenConnectionAsync(LocalDbConnectionString);
                        break;
                    case "SqlConnectionOpenAsyncFailedInvalidAccount":
                        SqlCommandHelper.OpenConnectionAsync(InvalidAccountConnectionString);
                        break;
                    case "SqlConnectionOpenAsyncFailedInvalidServer":
                        SqlCommandHelper.OpenConnectionAsync(InvalidServerConnectionString);
                        break;
                    case "SqlConnectionOpenAsyncAwait":
                        SqlCommandHelper.OpenConnectionAsyncAwait(LocalDbConnectionString);
                        break;
                    case "SqlConnectionOpenAsyncAwaitFailedInvalidAccount":
                        SqlCommandHelper.OpenConnectionAsyncAwait(InvalidAccountConnectionString);
                        break;
                    case "SqlConnectionOpenAsyncAwaitFailedInvalidServer":
                        SqlCommandHelper.OpenConnectionAsyncAwait(InvalidServerConnectionString);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Request Parameter type is not mapped to an action: " + type);
                }
            }
            catch (Exception ex)
            {
                this.lblRequestedAction.Text = this.lblRequestedAction.Text + "  Exception occured: " + ex;
            }

            TelemetryConfiguration.Active.TelemetryChannel.Flush();
        }
    }
}