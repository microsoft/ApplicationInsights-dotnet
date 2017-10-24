using System;

namespace FuncTest.Helpers
{
    internal class HttpTestConstants
    {
        /// <summary>
        /// Query string to specify Outbound HTTP Call .
        /// </summary>
        internal static string QueryStringOutboundHttp = "?type=http&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call POST.
        /// </summary>
        internal static string QueryStringOutboundHttpPost = "?type=httppost&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call POST.
        /// </summary>
        internal static string QueryStringOutboundHttpPostFailed = "?type=failedhttppost&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call which fails.
        /// </summary>
        internal static string QueryStringOutboundHttpFailed = "?type=failedhttp&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call which fails.
        /// </summary>
        internal static string QueryStringOutboundHttpFailedAtDns = "?type=failedhttpinvaliddns&count=";

        /// <summary>
        /// Query string to specify Outbound Azure sdk Call .
        /// </summary>
        internal static string QueryStringOutboundAzureSdk = "?type=azuresdk{0}&count={1}";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url
        /// <c>http://msdn.microsoft.com/en-us/library/ms228967(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync1 = "?type=httpasync1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228967(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync1Failed = "?type=failedhttpasync1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228962(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync2 = "?type=httpasync2&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228962(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync2Failed = "?type=failedhttpasync2&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228968(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync3 = "?type=httpasync3&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228968(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync3Failed = "?type=failedhttpasync3&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync4 = "?type=httpasync4&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsync4Failed = "?type=failedhttpasync4&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsyncAwait1 = "?type=httpasyncawait1&count=";

        /// <summary>
        /// Query string to specify Outbound HTTP Call in async way as described in below url and which fails.
        /// <c>http://msdn.microsoft.com/en-us/library/ms228972(v=vs.110).aspx</c>
        /// </summary>
        internal static string QueryStringOutboundHttpAsyncAwait1Failed = "?type=failedhttpasyncawait1&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundExecuteReaderAsync = "?type=ExecuteReaderAsync&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundExecuteScalarAsync = "?type=ExecuteScalarAsync&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundExecuteReaderStoredProcedureAsync = "?type=ExecuteReaderStoredProcedureAsync&storedProcedureName=GetTopTenMessages&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundTestExecuteReaderTwiceWithTasks = "?type=TestExecuteReaderTwiceWithTasks&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundExecuteNonQueryAsync = "?type=ExecuteNonQueryAsync&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundExecuteXmlReaderAsync = "?type=ExecuteXmlReaderAsync&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundSqlCommandExecuteScalar = "?type=SqlCommandExecuteScalar&success=true&count=";

        /// <summary>
        /// Query string to specify Outbound SQL Call .
        /// </summary>
        internal static string QueryStringOutboundSqlCommandExecuteScalarError = "?type=SqlCommandExecuteScalar&success=false&count=";

        /// <summary>
        /// Maximum access time for the initial call - This includes an additional 1-2 delay introduced before the very first call by Profiler V2.
        /// </summary>        
        internal static TimeSpan AccessTimeMaxHttpInitial = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Maximum access time for calls after initial - This does not incur perf hit of the very first call.
        /// </summary>        
        internal static TimeSpan AccessTimeMaxHttpNormal = TimeSpan.FromSeconds(10);
    }
}
