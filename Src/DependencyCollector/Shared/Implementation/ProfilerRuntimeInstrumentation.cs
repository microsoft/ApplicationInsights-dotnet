namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using Microsoft.Diagnostics.Instrumentation.Extensions.Intercept;

    internal static class ProfilerRuntimeInstrumentation
    {
        internal static void DecorateProfilerForHttp(ref ProfilerHttpProcessing httpCallbacks)
        {
            // Decorates Http GetResponse, 0 params
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.GetResponse",
                httpCallbacks.OnBeginForGetResponse,
                httpCallbacks.OnEndForGetResponse,
                httpCallbacks.OnExceptionForGetResponse,
                isStatic: false);

            // Decorates Http GetRequestStream, 1 param
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.GetRequestStream",
                httpCallbacks.OnBeginForGetRequestStream,
                null,
                httpCallbacks.OnExceptionForGetRequestStream,
                isStatic: false);

            // Decorates Http BeginGetResponse, 2 params
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.BeginGetResponse",
                httpCallbacks.OnBeginForBeginGetResponse,
                null,
                null,
                isStatic: false);

            // Decorates Http EndGetResponse, 1 param
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.EndGetResponse",
                null,
                httpCallbacks.OnEndForEndGetResponse,
                httpCallbacks.OnExceptionForEndGetResponse,
                isStatic: false);

            // Decorates Http BeginGetRequestStream
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.BeginGetRequestStream",
                httpCallbacks.OnBeginForBeginGetRequestStream,
                null,
                null,
                isStatic: false);

            // Decorates Http EndGetRequestStream, 2 params
            Functions.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.EndGetRequestStream",
                null,
                null,
                httpCallbacks.OnExceptionForEndGetRequestStream,
                isStatic: false);
        }

        internal static void DecorateProfilerForSql(ref ProfilerSqlProcessing sqlCallbacks)
        {
            // Decorate Sql ExecuteNonQuery, 0 param
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteNonQuery",
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync,
                isStatic: false);

            // Decorate Sql ExecuteReader, 2 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteReader",
                sqlCallbacks.OnBeginForExecuteReader,
                sqlCallbacks.OnEndForExecuteReader,
                sqlCallbacks.OnExceptionForExecuteReader,
                isStatic: false);

            // Decorate Sql ExecuteReader, 0 params (we instrument 2 overloads of ExecuteReader because there are cases when methods get inlined or tail call optimized)
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteReader",
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync,
                isStatic: false);

            // Decorate Sql ExecuteScalar, 0 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteScalar",
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync,
                isStatic: false);

            // Decorate Sql ExecuteXmlReader, 0 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteXmlReader",
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync,
                isStatic: false);

            // Decorates Sql BeginExecuteNonQueryInternal, 4 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteNonQueryInternal",
                sqlCallbacks.OnBeginForBeginExecuteNonQueryInternal,
                null,
                null,
                isStatic: false);

            // Decorates Sql EndExecuteNonQueryInternal, 1 param
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteNonQueryInternal",
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync,
                isStatic: false);

            // Decorates Sql BeginExecuteReaderInternal, 5 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteReaderInternal",
                sqlCallbacks.OnBeginForBeginExecuteReaderInternal,
                null,
                null,
                isStatic: false);

            // Decorates Sql EndExecuteReaderInternal, 1 param
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteReaderInternal",
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync,
                isStatic: false);

            // Decorates Sql BeginExecuteXmlReaderInternal, 4 params
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteXmlReaderInternal",
                sqlCallbacks.OnBeginForBeginExecuteXmlReaderInternal,
                null,
                null,
                isStatic: false);

            // Decorates Sql EndExecuteXmlReaderInternal, 1 param
            Functions.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteXmlReaderInternal",
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync,
                isStatic: false);
        }
    }
}
