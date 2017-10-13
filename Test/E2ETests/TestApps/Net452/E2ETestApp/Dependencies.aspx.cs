using FW40Shared;
using FW45Shared;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace E2ETestApp
{
    public partial class Dependencies : System.Web.UI.Page
    {
        public const string LocalDbConnectionString = @"Server =sql-server;User Id = sa; Password=MSDNm4g4z!n4";
        private const string UrlTestWebApiGetCall = "http://e2etestwebapi:80/api/values";
        public const string UrlWhichThrowException = "http://e2etestwebapi:80/api/values/999";
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";        

        protected void Page_Load(object sender, EventArgs e)
        {
            int count = 1;
            var type = Request.QueryString["type"];
            this.lblRequestedAction.Text = "Requested Action:" + type;
            try
            {
                switch (type)
                {
                    case "azuretable":
                        try
                        {
                            DependencyCallHelpers.MakeAzureCallToWriteTableWithSdk(1);
                            DependencyCallHelpers.MakeAzureCallToReadTableWithSdk(1);
                            this.lblRequestedAction.Text = this.lblRequestedAction.Text + " Sucess!";
                        }
                        catch (Exception ex)
                        {
                            this.lblRequestedAction.Text = this.lblRequestedAction.Text + "  Exception occured: " + ex;
                        }
                        break;
                    case "sql":
                        try
                        {
                            using (var connection = new SqlConnection(LocalDbConnectionString))
                            {
                                connection.Open();
                                SqlCommand cmd = connection.CreateCommand();
                                cmd.CommandText = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabases";
                                object result = cmd.ExecuteScalar();
                            }
                        }
                        catch (Exception ex)
                        {
                            this.lblRequestedAction.Text = this.lblRequestedAction.Text + "  Exception occured: " + ex;
                        }
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
                        HttpHelper40.MakeAzureCallToReadBlobWithSdk();
                        break;
                    case "azuresdkqueue":
                        HttpHelper40.MakeAzureCallToWriteQueueWithSdk();
                        break;
                    case "azuresdktable":
                        HttpHelper40.MakeAzureCallToWriteTableWithSdk();
                        HttpHelper40.MakeAzureCallToReadTableWithSdk();
                        break; /*
                case "sql":
                    this.MakeSQLCallSync(count);
                    break;
                case "ExecuteReaderAsync":
                    SqlCommandHelper.ExecuteReaderAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteScalarAsync":
                    SqlCommandHelper.ExecuteScalarAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteReaderStoredProcedureAsync":
                    this.ExecuteReaderStoredProcedureAsync();
                    break;
                case "TestExecuteReaderTwice":
                    SqlCommandHelper.TestExecuteReaderTwice(ConnectionString, sqlQueryTouse);
                    break;
                case "BeginExecuteReader0":
                    SqlCommandHelper.BeginExecuteReader(ConnectionString, sqlQueryTouse, 0);
                    break;
                case "BeginExecuteReader1":
                    SqlCommandHelper.BeginExecuteReader(ConnectionString, sqlQueryTouse, 1);
                    break;
                case "BeginExecuteReader2":
                    SqlCommandHelper.BeginExecuteReader(ConnectionString, sqlQueryTouse, 2);
                    break;
                case "BeginExecuteReader3":
                    SqlCommandHelper.BeginExecuteReader(ConnectionString, sqlQueryTouse, 3);
                    break;
                case "TestExecuteReaderTwiceInSequence":
                    SqlCommandHelper.TestExecuteReaderTwiceInSequence(ConnectionString, sqlQueryTouse);
                    break;
                case "TestExecuteReaderTwiceWithTasks":
                    SqlCommandHelper.AsyncExecuteReaderInTasks(ConnectionString, sqlQueryTouse);
                    break;
                case "ExecuteNonQueryAsync":
                    SqlCommandHelper.ExecuteNonQueryAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "BeginExecuteNonQuery0":
                    SqlCommandHelper.BeginExecuteNonQuery(ConnectionString, sqlQueryTouse, 0);
                    break;
                case "BeginExecuteNonQuery2":
                    SqlCommandHelper.BeginExecuteNonQuery(ConnectionString, sqlQueryTouse, 2);
                    break;
                case "ExecuteXmlReaderAsync":
                    sqlQueryTouse += " FOR XML AUTO";
                    SqlCommandHelper.ExecuteXmlReaderAsync(ConnectionString, sqlQueryTouse);
                    break;
                case "BeginExecuteXmlReader":
                    SqlCommandHelper.BeginExecuteXmlReader(ConnectionString, sqlQueryTouse);
                    break;
                case "SqlCommandExecuteScalar":
                    sqlQueryTouse = (success == true)
                                    ? ValidSqlQueryCountToApmDatabase
                                    : InvalidSqlQueryToApmDatabase;
                    SqlCommandHelper.ExecuteScalar(ConnectionString, sqlQueryTouse);
                    break;
                case "SqlCommandExecuteNonQuery":
                    sqlQueryTouse = (success == true)
                           ? ValidSqlQueryCountToApmDatabase
                           : InvalidSqlQueryToApmDatabase;
                    SqlCommandHelper.ExecuteNonQuery(ConnectionString, sqlQueryTouse);
                    break;
                case "SqlCommandExecuteReader0":
                    SqlCommandHelper.ExecuteReader(ConnectionString, sqlQueryTouse, 0);
                    break;
                case "SqlCommandExecuteReader1":
                    SqlCommandHelper.ExecuteReader(ConnectionString, sqlQueryTouse, 1);
                    break;
                case "SqlCommandExecuteXmlReader":
                    sqlQueryTouse += " FOR XML AUTO";
                    SqlCommandHelper.ExecuteXmlReader(ConnectionString, sqlQueryTouse);
                    break;
                case "SqlConnectionOpen":
                    sqlQueryTouse = "Open";
                    SqlCommandHelper.OpenConnection(this.GetConnectionString(success, Request.QueryString["exceptionType"]));
                    break;
                case "SqlConnectionOpenAsync":
                    sqlQueryTouse = "Open";
                    SqlCommandHelper.OpenConnectionAsync(this.GetConnectionString(success, Request.QueryString["exceptionType"]));
                    break;
                case "SqlConnectionOpenAsyncAwait":
                    sqlQueryTouse = "Open";
                    SqlCommandHelper.OpenConnectionAsyncAwait(this.GetConnectionString(success, Request.QueryString["exceptionType"]));
                    break; */
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