namespace FW45Shared
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;

    public class SqlCommandHelper
    {
        #region ExecuteReader
        
        public static async void ExecuteReaderAsync(string connectionString, string commandText, CommandType commandType = CommandType.Text)
        {
            await ExecuteReaderAsyncInternal(connectionString, commandText, commandType);
        }

        public static void BeginExecuteReader(string connectionString, string commandText)
        {
           ManualResetEvent mre = new ManualResetEvent(false);

           var executor = new AsyncExecuteReaderWrapper(connectionString, commandText, mre);
            executor.BeginExecute();

            mre.WaitOne(1000);
        }

        public static void TestExecuteReaderTwice(string connectionString, string commandText)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteReaderInParallel(connectionString, commandText, mre);
            executor.BeginExecute();
            mre.WaitOne(1000);
        }

        public static void TestExecuteReaderTwiceInSequence(string connectionString, string commandText)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteReaderInSequence(connectionString, commandText, mre);
            executor.BeginExecute();
            mre.WaitOne(1000);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void AsyncExecuteReaderInTasks(string connectionString, string commandText)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            var task1 = command.ExecuteReaderAsync();
            var task2 = command.ExecuteReaderAsync();
            task1.Wait();
            task2.Wait();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void ExecuteReader(string connectionString, string commandText, CommandType commandType = CommandType.Text)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;
                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            // Process each column as appropriate
                            object obj = reader.GetFieldValue<object>(i);
                        }
                    }
                }
            }
        }

        #endregion

        #region ExecuteNonQuery
        
        public static async void ExecuteNonQueryAsync(string connectionString, string commandText)
        {
            await ExecuteNonQueryAsyncInternal(connectionString, commandText);
        }

        public static void BeginExecuteNonQuery(string connectionString, string commandText)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteNonQueryWrapper(connectionString, commandText, mre);
            executor.BeginExecute();
            mre.WaitOne(1000);

        }

        public static void BeginExecuteNonQueryProc(string connectionString, string commandText)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteNonQueryWrapper(connectionString, commandText, mre);
            executor.BeginExecuteProc();
            mre.WaitOne(1000);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void ExecuteNonQuery(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.ExecuteNonQuery();
            }
        }

        #endregion

        #region ExecuteScalar
        public static async void ExecuteScalarAsync(string connectionString, string commandText)
        {
            await ExecuteScalarInternal(connectionString, commandText);            
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static object ExecuteScalar(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                object obj = command.ExecuteScalar();
                return obj;
            }
        }

        #endregion

        #region ExecuteXmlReader

        public static async void ExecuteXmlReaderAsync(string connectionString, string commandText)
        {
            await ExecuteXmlReaderAsyncInternal(connectionString, commandText);
        }

        public static void BeginExecuteXmlReader(string connectionString, string commandText)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteXmlReaderWrapper(connectionString, commandText, mre);
            executor.BeginExecute();
            mre.WaitOne(1000);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void ExecuteXmlReader(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                using (var reader = command.ExecuteXmlReader())
                {
                }
            }    
        }

        #endregion

        private async static Task ExecuteReaderAsyncInternal(string connectionString, string commandText, CommandType commandType)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;
                using (DbDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            // Process each column as appropriate
                            object obj = await reader.GetFieldValueAsync<object>(i);
                        }
                    }
                }
            }
        }

        private async static Task<object> ExecuteScalarInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                object obj = await command.ExecuteScalarAsync();
                return obj;
            }
        }

        private async static Task ExecuteNonQueryAsyncInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                await command.ExecuteNonQueryAsync();
            }
        }

        private async static Task ExecuteXmlReaderAsyncInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                using (var reader = await command.ExecuteXmlReaderAsync())
                {
                }
            }
        }

        private sealed class AsyncExecuteReaderWrapper : IDisposable
        {
            private readonly SqlCommand command;

            private readonly SqlConnection connection;

            private ManualResetEvent mre;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteReaderWrapper(string connectionString, string commandText, ManualResetEvent mre)
            {
                this.mre = mre;
                this.connection =new SqlConnection(connectionString);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                command.CommandText = commandText;
            }

            public void BeginExecute()
            {
                try
                {
                    this.command.BeginExecuteReader(Callback, null);
                    
                }
                catch(Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }                
            }
            public void Dispose()
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                }
            }
            private void Callback(IAsyncResult ar)
            {
                this.command.EndExecuteReader(ar);
                this.connection.Close();
                this.mre.Set();
            }
        }

        private sealed class AsyncExecuteReaderInParallel : IDisposable
        {
             private readonly SqlCommand command;

            private readonly SqlConnection connection;

            private ManualResetEvent mre;
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteReaderInParallel(string connectionString, string commandText, ManualResetEvent mre)
            {
                this.mre = mre;
                this.connection =new SqlConnection(connectionString);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                command.CommandText = commandText;
            }

            public void BeginExecute()
            {                
                try
                {
                    command.BeginExecuteReader(Callback1, null);
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }   
            }
            public void Dispose()
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                }
            }
            private void Callback1(IAsyncResult ar1)
            {
                this.command.BeginExecuteReader(this.Callback2, null);
                this.command.EndExecuteReader(ar1);
            }

            private void Callback2(IAsyncResult ar2)
            {
                this.command.EndExecuteReader(ar2);
                this.connection.Close();
                this.mre.Set();
            }
        }

        private sealed class AsyncExecuteReaderInSequence : IDisposable
        {
            private readonly SqlCommand command;

            private readonly SqlConnection connection;
            private ManualResetEvent mre;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteReaderInSequence(string connectionString, string commandText, ManualResetEvent mre)
            {
                this.mre = mre;
                this.connection = new SqlConnection(connectionString);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                command.CommandText = commandText;
            }

            public void BeginExecute()
            {                
                try
                {
                    command.BeginExecuteReader(Callback1, null);
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }   
            }
            public void Dispose()
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                }
            }
            private void Callback1(IAsyncResult ar1)
            {
                this.command.EndExecuteReader(ar1);
                this.command.BeginExecuteReader(this.Callback2, null);
            }

            private void Callback2(IAsyncResult ar2)
            {
                this.command.EndExecuteReader(ar2);
                this.connection.Close();
                this.mre.Set();
            }
        }

        private sealed class AsyncExecuteNonQueryWrapper : IDisposable
        {
            private readonly SqlCommand command;

            private readonly SqlConnection connection;
            private ManualResetEvent mre;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteNonQueryWrapper(string connectionString, string commandText, ManualResetEvent mre)
            {
                this.mre = mre;
                this.connection = new SqlConnection(connectionString);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                command.CommandText = commandText;
            }

            public void BeginExecute()
            {
                try
                {
                    this.command.BeginExecuteNonQuery(Callback, null); 
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                } 
            }

            public void BeginExecuteProc()
            {
                try
                {
                    this.command.CommandType = CommandType.StoredProcedure;                    
                    this.command.BeginExecuteNonQuery(Callback, null);
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                }
            }

            public void Dispose()
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                }
            }
            private void Callback(IAsyncResult ar)
            {
                this.command.EndExecuteNonQuery(ar);
                this.connection.Close();
                this.mre.Set();
            }
        }

        private sealed class AsyncExecuteXmlReaderWrapper : IDisposable
        {
            private readonly SqlCommand command;
            private readonly SqlConnection connection;
            private ManualResetEvent mre;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteXmlReaderWrapper(string connectionString, string commandText, ManualResetEvent mre)
            {
                this.mre = mre;
                this.connection = new SqlConnection(connectionString);
                this.connection.Open();
                this.command = this.connection.CreateCommand();
                command.CommandText = commandText;
            }

            public void BeginExecute()
            {
                try
                {
                    this.command.BeginExecuteXmlReader(Callback, null);
                }
                catch (Exception)
                {
                    if (connection != null)
                    {
                        connection.Close();
                    }
                } 
            }

            public void Dispose()
            {
                if (this.connection != null)
                {
                    this.connection.Dispose();
                }
            }

            private void Callback(IAsyncResult ar)
            {
                this.command.EndExecuteNonQuery(ar);
                this.connection.Close();
                this.mre.Set();
            }          
        }
    }
}