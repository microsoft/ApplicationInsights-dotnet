// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExternalCalls.aspx.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Aspx451
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using FW40Shared;
    using FW45Shared;

    /// <summary>
    /// ExternalCalls page to make remote dependency calls.
    /// </summary>
    [ComVisible(false)]
    public partial class ExternalCalls : System.Web.UI.Page
    {
        /// <summary>
        /// Connection string to APM Development database.
        /// </summary> 
        private const string ConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=RDDTestDatabase;Integrated Security=True";

        /// <summary>
        /// Invalid connection string to database.
        /// </summary> 
        private const string InvalidConnectionString = @"Data Source=invalid\SQLEXPRESS;Initial Catalog=RDDTestDatabase;Integrated Security=True";

        /// <summary>
        /// Connection string to database with invalid account.
        /// </summary> 
        private const string InvalidAccountConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog=RDDTestDatabase;User ID = AiUser;Password=Some";        

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

        /// <summary>
        /// ExternalCalls page to make remote dependency calls.
        /// </summary>        
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Argument object.</param>        
        protected void Page_Load(object sender, EventArgs e)
        {
            var type = Request.QueryString["type"];
            var countStr = Request.QueryString["count"];
            var success = true;
            bool.TryParse(Request.QueryString["success"], out success);
            string sqlQueryTouse = (success == true) ? ValidSqlQueryToApmDatabase : InvalidSqlQueryToApmDatabase;
            var count = 1;
            try
            {
                count = int.Parse(countStr, CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                // Dont care about this           
            }

            this.lblRequestedAction.Text = "Requested Action:" + type;

            try
            {
                switch (type)
                {
                    case "http":
                        HttpHelper40.MakeHttpCallSync(count, "bing");
                        break;
                    case "httpClient":
                        HttpHelper40.MakeHttpCallUsingHttpClient("http://www.google.com/404");
                        break;
                    case "httppost":
                        HttpHelper40.MakeHttpPostCallSync(count, "bing");
                        break;
                    case "failedhttp":
                        HttpHelper40.MakeHttpCallSyncFailed(count);
                        break;
                    case "failedhttpinvaliddns":
                        HttpHelper40.MakeHttpCallSyncFailed(count, true);
                        break;
                    case "httpasync1":
                        HttpHelper40.MakeHttpCallAsync1(count, "bing");
                        break;
                    case "failedhttpasync1":
                        HttpHelper40.MakeHttpCallAsync1Failed(count);
                        break;
                    case "httpasync2":
                        HttpHelper40.MakeHttpCallAsync2(count, "bing");
                        break;
                    case "failedhttpasync2":
                        HttpHelper40.MakeHttpCallAsync2Failed(count);
                        break;
                    case "httpasync3":
                        HttpHelper40.MakeHttpCallAsync3(count, "bing");
                        break;
                    case "failedhttpasync3":
                        HttpHelper40.MakeHttpCallAsync3Failed(count);
                        break;
                    case "httpasync4":
                        HttpHelper40.MakeHttpCallAsync4(count, "bing");
                        break;
                    case "failedhttpasync4":
                        HttpHelper40.MakeHttpCallAsync4Failed(count);
                        break;
                    case "httpasyncawait1":
                        HttpHelper45.MakeHttpCallAsyncAwait1(count, "bing");
                        break;
                    case "failedhttpasyncawait1":
                        HttpHelper45.MakeHttpCallAsyncAwait1Failed(count);
                        break;
                    case "sql":
                        this.MakeSQLCallSync(count);
                        break;
                    case "azuresdkblob":
                        HttpHelper40.MakeAzureCallToReadBlobWithSdk(count);
                        break;
                    case "azuresdkqueue":
                        HttpHelper40.MakeAzureCallToWriteQueueWithSdk(count);
                        break;
                    case "azuresdktable":
                        HttpHelper40.MakeAzureCallToWriteTableWithSdk(count);
                        HttpHelper40.MakeAzureCallToReadTableWithSdk(count);
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
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("Request Parameter type is not mapped to an action: " + type);
                }

                this.lblResult.Text = "Requested action completed successfully.";
                Response.Write(QueryToExecuteLabel + sqlQueryTouse);
            }
            catch (Exception ex)
            {
                Response.Write(QueryToExecuteLabel + sqlQueryTouse);
                this.lblResult.Text = "The following error occured while attempting to perform requested action" + ex;
            }
        }

        /// <summary>
        /// Returns connection string.
        /// </summary>        
        /// <param name="success">Success flag.</param>        
        /// <param name="exceptionType">Exception type.</param>        
        /// <returns>Connection string.</returns>
        private string GetConnectionString(bool success, string exceptionType)
        {
            string result = ConnectionString;
            if (!success)
            {
                result = exceptionType.Equals("account", StringComparison.OrdinalIgnoreCase) ? InvalidAccountConnectionString : InvalidConnectionString;
            }

            return result;
        }

        /// <summary>
        /// Make sync SQL calls.
        /// </summary>        
        /// <param name="count">No of calls to be made.</param>        
        private void MakeSQLCallSync(int count)
        {
            SqlConnection conn = null;
            SqlCommand cmd = null;
            SqlDataReader rdr = null;
            for (int i = 0; i < count; i++)
            {
                conn = new SqlConnection(ConnectionString);
                conn.Open();
                cmd = new SqlCommand("GetTopTenMessages", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                rdr = cmd.ExecuteReader();
                rdr.Close();
            }
        }

        /// <summary>
        /// Executes reader stored procedure.
        /// </summary>        
        private void ExecuteReaderStoredProcedureAsync()
        {
            var storedProcedureName = this.Request.QueryString["storedProcedureName"];
            if (string.IsNullOrEmpty(storedProcedureName))
            {
                throw new ArgumentException("storedProcedureName query string parameter is not defined.");
            }

            SqlCommandHelper.ExecuteReaderAsync(ConnectionString, storedProcedureName, CommandType.StoredProcedure);
        }
    }
}