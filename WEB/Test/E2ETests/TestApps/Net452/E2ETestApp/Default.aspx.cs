using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;

namespace E2ETestApp
{
    public partial class _Default : Page
    {
        private const long RequestCountToThrowException = 10;
        private const long RequestCountToSwitchDependencyType = 2;
        private const long RequestCountToSwitchSQLQuery = 5;
        private const long RequestCountToSQLFailedQuery = 23;
        private const long MillisecondsInUselessCycle = 30;                
        public const string LocalDbConnectionString = @"Server =sql-server;User Id = sa; Password=MSDNm4g4z!n4"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]
        private static long requestCount;
        private static string whatHappened = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            whatHappened = "PageLoaded. ";
            long currentRequest = Interlocked.Increment(ref requestCount);
            whatHappened = whatHappened + "CurrentRequest:" + currentRequest;

            Uri currentRequestUri = this.Context.Request.Url;
            this.CallDependencyForRequest(currentRequest, currentRequestUri);
            this.TraceEvent(currentRequest);
            this.CyclecForMilliseconds(MillisecondsInUselessCycle);
            this.ThrowExceptionIfNeededForRequest(currentRequest);

            lblWhatHappened.Text = whatHappened;

            TelemetryConfiguration.Active.TelemetryChannel.Flush();
        }

        private void CallDependencyForRequest(long requestId, Uri requestUri)
        {
            if (requestId % RequestCountToSwitchDependencyType == 0)
            {
                whatHappened = whatHappened + "CallHttp. ";
                CallHttp();
                whatHappened = whatHappened + "CallHttp. ";
            }
            else
            {
                whatHappened = whatHappened + "CallSQL. ";
                CallSql(requestId);
            }
        }

        private void CallSql(long requestId)
        {
            using (var connection = new SqlConnection(LocalDbConnectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                if (requestId % RequestCountToSQLFailedQuery == 0)
                {
                    cmd.CommandText = "WAITFOR DELAY '00:00:01:006';SELECT * FROM UnknownTable";
                }
                else if (requestId % RequestCountToSwitchSQLQuery == 0)
                {
                    cmd.CommandText = "WAITFOR DELAY '00:00:00:007';SELECT name FROM master.dbo.sysdatabases";
                }
                else
                {
                    cmd.CommandText = "WAITFOR DELAY '00:00:00:009';SELECT name FROM master.dbo.sysdatabases";
                }

                object result = cmd.ExecuteScalar();
            }
        }

        private void CallHttp()
        {
            Uri perftext1 = new Uri("http://e2etestwebapi:80/api/values");
            Uri perftext2 = new Uri("http://e2etestwebapi:80/api/values/3");
            HttpClient client = new HttpClient();
            Task[] tasks = new Task[2];

            tasks[0] = client.GetAsync(perftext1);
            tasks[1] = client.GetAsync(perftext2);

            Task.WaitAll(tasks);
        }

        private void ThrowExceptionIfNeededForRequest(long requestId)
        {
            if (requestId % RequestCountToThrowException == 0)
            {
                throw new Exception("Time to fail!");
            }
        }

        private void TraceEvent(long requestId)
        {
            /*
            whatHappened = whatHappened + "TraceEvented. ";
            TelemetryClient tc = new TelemetryClient();

            for (int i = 1; i <= 2; i++)
            {
                var props = new Dictionary<string, string>();
                props.Add("myAIKey", TelemetryConfiguration.Active.InstrumentationKey);
                props.Add("loopCount", i.ToString());

                var mets = new Dictionary<string, double>();
                mets.Add("RequestID", requestId);
                tc.TrackEvent("Invoked", props, mets);
            }
            */
        }

        private void CyclecForMilliseconds(long N)
        {
            whatHappened = whatHappened + "CycledForNoJob. ";
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.ElapsedMilliseconds < N && stopWatch.IsRunning) { Guid guid = Guid.NewGuid(); }
            stopWatch.Stop();
        }
    }
}