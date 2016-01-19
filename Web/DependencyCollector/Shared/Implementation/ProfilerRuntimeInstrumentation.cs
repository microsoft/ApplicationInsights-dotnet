namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using Microsoft.Diagnostics.Instrumentation.Extensions.Intercept;

    internal static class ProfilerRuntimeInstrumentation
    {
        internal static void DecorateProfilerForHttp(ref ProfilerHttpProcessing httpCallbacks)
        {
            // Decorates Http GetResponse
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.GetResponse",
                0,
                httpCallbacks.OnBeginForGetResponse,
                httpCallbacks.OnEndForGetResponse,
                httpCallbacks.OnExceptionForGetResponse);

            // Decorates Http GetRequestStream
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.GetRequestStream",
                1,
                httpCallbacks.OnBeginForGetRequestStream,
                null,
                httpCallbacks.OnExceptionForGetRequestStream);

            // Decorates Http BeginGetResponse
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.BeginGetResponse",
                2,
                httpCallbacks.OnBeginForBeginGetResponse,
                null,
                null);

            // Decorates Http EndGetResponse
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.EndGetResponse",
                1,
                null,
                httpCallbacks.OnEndForEndGetResponse,
                httpCallbacks.OnExceptionForEndGetResponse);

            // Decorates Http BeginGetRequestStream
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.BeginGetRequestStream",
                2,
                httpCallbacks.OnBeginForBeginGetRequestStream,
                null,
                null);

            // Decorates Http EndGetRequestStream
            Decorator.Decorate(
                "System",
                "System.dll",
                "System.Net.HttpWebRequest.EndGetRequestStream",
                2,
                null,
                null,
                httpCallbacks.OnExceptionForEndGetRequestStream);
        }

        internal static void DecorateProfilerForSql(ref ProfilerSqlProcessing sqlCallbacks)
        {
            // Decorate Sql ExecuteNonQuery
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteNonQuery",
                0,
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync);
           
            // Decorate Sql ExecuteReader
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteReader",
                2,
                sqlCallbacks.OnBeginForExecuteReader,
                sqlCallbacks.OnEndForExecuteReader,
                sqlCallbacks.OnExceptionForExecuteReader);

            // Decorate Sql ExecuteReader (we instrument 2 overloads of ExecuteReader because there are cases when methods get inlined or tail call optimized)
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteReader",
                0,
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync);

            // Decorate Sql ExecuteScalar
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteScalar",
                0,
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync);

            // Decorate Sql ExecuteXmlReader
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.ExecuteXmlReader",
                0,
                sqlCallbacks.OnBeginForSync,
                sqlCallbacks.OnEndForSync,
                sqlCallbacks.OnExceptionForSync); 

            // Decorates Sql BeginExecuteNonQueryInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteNonQueryInternal",
                4,
                sqlCallbacks.OnBeginForBeginExecuteNonQueryInternal,
                null,
                null);

            // Decorates Sql EndExecuteNonQueryInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteNonQueryInternal",
                1,
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync);
            
            // Decorates Sql BeginExecuteReaderInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteReaderInternal",
                5,
                sqlCallbacks.OnBeginForBeginExecuteReaderInternal,
                null,
                null);

            // Decorates Sql EndExecuteReaderInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteReaderInternal",
                1,
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync);           

            // Decorates Sql BeginExecuteXmlReaderInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.BeginExecuteXmlReaderInternal",
                4,
                sqlCallbacks.OnBeginForBeginExecuteXmlReaderInternal,
                null,
                null);

            // Decorates Sql EndExecuteXmlReaderInternal
            Decorator.Decorate(
                "System.Data",
                "System.Data.dll",
                "System.Data.SqlClient.SqlCommand.EndExecuteXmlReaderInternal",
                1,
                null,
                sqlCallbacks.OnEndForSqlAsync,
                sqlCallbacks.OnExceptionForSqlAsync);
        }
    }
}
