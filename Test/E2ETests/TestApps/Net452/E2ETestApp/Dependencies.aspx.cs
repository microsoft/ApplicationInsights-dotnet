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
        public const string LocalDbConnectionString = @"Server =sql-server;User Id = sa; Password=MSDNm4g4z!n3";

        protected void Page_Load(object sender, EventArgs e)
        {
            var type = Request.QueryString["type"];
            this.lblRequestedAction.Text = "Requested Action:" + type;
            switch (type)
            {
                case "http":
                    HttpClient client = new HttpClient();
                    client.GetAsync(new Uri("http://e2etestwebapi:80/api/values")).Wait();
                    break;
                case "sql":
                    using (var connection = new SqlConnection(LocalDbConnectionString))
                    {
                        connection.Open();
                        SqlCommand cmd = connection.CreateCommand();                        
                        cmd.CommandText = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabases";                        
                        object result = cmd.ExecuteScalar();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Request Parameter type is not mapped to an action: " + type);
            }

            TelemetryConfiguration.Active.TelemetryChannel.Flush();
        }
    }
}