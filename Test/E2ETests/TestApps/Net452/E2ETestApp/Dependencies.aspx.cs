using HttpSQLHelpers;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
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
        public static string ConnectionStringFormat = "Server = {0};Initial Catalog=dependencytest;User Id = sa; Password=MSDNm4g4z!n4"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]
        public static string LocalDbConnectionString;
        public const string InvalidAccountConnectionString = @"Server =sql-server;User Id = sa; Password=thisiswrong"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]
        public const string InvalidServerConnectionString = @"Server =sql-server-dontexist;User Id = sa; Password=MSDNm4g4z!n4"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]
        private string SqlQuerySuccess = "WAITFOR DELAY '00:00:00:007';select * from dbo.Messages";
        private string SqlQueryError = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabasesunknown";        
        public static string EndPointAddressFormat = "http://{0}/api/Data/PushItem";

        private const string UrlTestWebApiGetCallTemplate = "http://{0}:80/api/values";
        private const string UrlGoogle = "http://google.com";
        public const string UrlWhichThrowExceptionFormat = "http://{0}:80/api/values/999";
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";
        private static bool etwEnabled = false;

        protected void Page_Load(object sender, EventArgs e)
        {
            var type = Request.QueryString["type"];
            var success = true;
            bool.TryParse(Request.QueryString["success"], out success);
            this.lblRequestedAction.Text = "Requested Action:" + type;

            var webApiHostName = Microsoft.Azure.CloudConfigurationManager.GetSetting("webapihostname");
            string UrlTestWebApiGetCall = string.Format(UrlTestWebApiGetCallTemplate, webApiHostName);
            string UrlWhichThrowException = string.Format(UrlWhichThrowExceptionFormat, webApiHostName);

            var ingestionhostname = Microsoft.Azure.CloudConfigurationManager.GetSetting("ingestionhostname");
            TelemetryConfiguration.Active.TelemetryChannel.EndpointAddress = string.Format(EndPointAddressFormat, ingestionhostname);

            var sqlhostname = Microsoft.Azure.CloudConfigurationManager.GetSetting("sqlhostname");
            LocalDbConnectionString = string.Format(ConnectionStringFormat, sqlhostname);

            try
            {
                switch (type)
                {
                    // A bit hacky way to enable collect application logs. This is temporary until actual
                    // etw listener can be installed inside docker itself separate from the app.
                    case "etw":
                        if(etwEnabled)                        
                            break;
                        

                        string levelString = "Error";
                        try {
                            levelString = Request.QueryString["level"];
                        }
                        catch (Exception) { }

                        EventLevel level = EventLevel.Error;
                        switch (levelString)
                        {
                            case "Verbose": level = EventLevel.Verbose; break;
                            case "Informational": level = EventLevel.Informational; break;
                            case "Warning": level = EventLevel.Warning; break;
                            case "Error": level = EventLevel.Error; break;
                            case "Critical": level = EventLevel.Critical; break;
                        }
                        new Thread(() =>
                        {
                            try
                            {
                                Thread.CurrentThread.IsBackground = true;
                                DiagnosticsEventListener dd = new DiagnosticsEventListener(level);
                            }
                            catch(Exception)
                            {

                            }
                        }).Start();
                        etwEnabled = true;
                        TelemetryConfiguration.Active.TelemetryChannel.Flush();
                        Thread.Sleep(1000);
                        break;
                    case "etwlogs":
                        string MyDirectoryPath = "c:\\mylogs";
                        string filename = "logs.txt";                         

                        using (TextReader reader = new StreamReader(File.OpenRead(Path.Combine(MyDirectoryPath, filename))))
                        {
                            Response.Write(reader.ReadToEnd()); 
                        }

                        File.Delete(Path.Combine(MyDirectoryPath, filename));

                        break;

                    case "flush":
                            TelemetryConfiguration.Active.TelemetryChannel.Flush();
                            Thread.Sleep(3000);
                            break;
                    case "httpsyncgoogle":
                        HttpHelpers.MakeHttpCallSync(UrlGoogle);
                        break;
                    case "httpsync":
                        HttpHelpers.MakeHttpCallSync(UrlTestWebApiGetCall);
                        break;
                    case "httpasynchttpclient":
                        HttpHelpers.MakeHttpCallUsingHttpClient(UrlTestWebApiGetCall);
                        break;
                    case "httppost":
                        HttpHelpers.MakeHttpPostCallSync(UrlTestWebApiGetCall);
                        break;
                    case "httpfailedwithexception":
                        HttpHelpers.MakeHttpCallUsingHttpClient(UrlWhichThrowException);
                        break;
                    case "httpfailedwithinvaliddns":
                        HttpHelpers.MakeHttpCallUsingHttpClient(UrlWithNonexistentHostName);
                        break;
                    case "httpasync1":
                        HttpHelpers.MakeHttpCallAsync1(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync1":
                        HttpHelpers.MakeHttpCallAsync1(UrlWhichThrowException);
                        break;
                    case "httpasync2":
                        HttpHelpers.MakeHttpCallAsync2(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync2":
                        HttpHelpers.MakeHttpCallAsync2(UrlWhichThrowException);
                        break;
                    case "httpasync3":
                        HttpHelpers.MakeHttpCallAsync3(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync3":
                        HttpHelpers.MakeHttpCallAsync3(UrlWhichThrowException);
                        break;
                    case "httpasync4":
                        HttpHelpers.MakeHttpCallAsync4(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasync4":
                        HttpHelpers.MakeHttpCallAsync4(UrlWhichThrowException);
                        break;
                    case "httpasyncawait1":
                        HttpHelpers.MakeHttpCallAsyncAwait1(UrlTestWebApiGetCall);
                        break;
                    case "failedhttpasyncawait1":
                        HttpHelpers.MakeHttpCallAsyncAwait1(UrlWhichThrowException);
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
                        HttpHelpers.MakeAzureCallToWriteToBlobWithSdk(containerName, blobName);
                        HttpHelpers.MakeAzureCallToReadBlobWithSdk(containerName, blobName);
                        break;
                    case "azuresdkqueue":
                        HttpHelpers.MakeAzureCallToWriteQueueWithSdk();
                        break;
                    case "azuresdktable":
                        string tableName = "people";
                        try
                        {
                            tableName = Request.QueryString["tablename"];
                        }
                        catch (Exception) { }
                        HttpHelpers.MakeAzureCallToWriteTableWithSdk(tableName);
                        HttpHelpers.MakeAzureCallToReadTableWithSdk(tableName);
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
                        string storedProcedureName = "GetTopTenMessages";
                        try
                        {
                            storedProcedureName = Request.QueryString["storedProcedureName"];
                        }
                        catch (Exception) { }
                        SqlCommandHelper.ExecuteReaderAsync(LocalDbConnectionString, storedProcedureName, CommandType.StoredProcedure);
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
                        SqlQuerySuccess += " FOR XML AUTO";
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