namespace HttpSQLHelpers
{
    using System;
    using System.Data;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Threading.Tasks;

    public class SqlCommandHelper
    {
        #region SqlConnection

        public static void OpenConnection(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
            }
        }

        public static void OpenConnectionAsync(string connectionString)
        {
            var connection = new SqlConnection(connectionString);
            connection.OpenAsync();
        }

        public static async void OpenConnectionAsyncAwait(string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
            }
        }

        #endregion

        #region ExecuteReader

        public static async void ExecuteReaderAsync(string connectionString, string commandText, CommandType commandType = CommandType.Text)
        {
            await ExecuteReaderAsyncInternal(connectionString, commandText, commandType);
        }

#if !NETCOREAPP2_0 && !NETCOREAPP1_0
        public static void BeginExecuteReader(string connectionString, string commandText, int numberOfAsyncArgs)
        {
            ManualResetEvent mre = new ManualResetEvent(false);

            var executor = new AsyncExecuteReaderWrapper(connectionString, commandText, mre, numberOfAsyncArgs);
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
#endif

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        public static void AsyncExecuteReaderInTasks(string connectionString, string commandText)
        {
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = commandText;

            // TODO: There is race condition here. SqlConnection is not threadsafe
            // and these tasks need to be serialized.
            var task1 = command.ExecuteReaderAsync();
            var task2 = command.ExecuteReaderAsync();

            task1.Wait();
            task2.Wait();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security",
            "CA2100:Review SQL queries for security vulnerabilities")]
        public static void ExecuteReader(string connectionString, string commandText, int numberOfArgs,
            CommandType commandType = CommandType.Text)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = commandText;
                command.CommandType = commandType;

                DbDataReader reader = null;

                try
                {
                    reader = numberOfArgs == 0
                        ? command.ExecuteReader()
                        : command.ExecuteReader(CommandBehavior.SequentialAccess);
                }
                finally
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

#if !NETCOREAPP2_0 && !NETCOREAPP1_0
        public static void BeginExecuteNonQuery(string connectionString, string commandText, int numberOfArgs)
        {
            ManualResetEvent mre = new ManualResetEvent(false);
            var executor = new AsyncExecuteNonQueryWrapper(connectionString, commandText, mre, numberOfArgs);
            executor.BeginExecute();
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
#endif

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
                object obj = null;
                try
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    obj = command.ExecuteScalar();                    
                } catch(Exception)
                {

                }
                return obj;
            }
        }

        #endregion

        #region ExecuteXmlReader

        public static async void ExecuteXmlReaderAsync(string connectionString, string commandText)
        {
            await ExecuteXmlReaderAsyncInternal(connectionString, commandText);
        }

#if !NETCOREAPP2_0 && !NETCOREAPP1_0
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
#endif

        #endregion

        private async static Task ExecuteReaderAsyncInternal(string connectionString, string commandText, CommandType commandType)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
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
                }catch(Exception)
                {
                 // dont do anything.   
                }
            }
        }

        private async static Task<object> ExecuteScalarInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                object obj = null;
                try
                {
                    await connection.OpenAsync();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    obj = await command.ExecuteScalarAsync();                    
                }
                catch(Exception)
                {

                }
                return obj;
            }
        }

        private async static Task ExecuteNonQueryAsyncInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    await command.ExecuteNonQueryAsync();
                }
                catch(Exception)
                {

                }                
            }
        }

        private async static Task ExecuteXmlReaderAsyncInternal(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = commandText;
                    using (var reader = await command.ExecuteXmlReaderAsync())
                    {
                    }
                }    
                catch(Exception)
                {

                }
            }
        }

#if !NETCOREAPP2_0 && !NETCOREAPP1_0
        private sealed class AsyncExecuteReaderWrapper : IDisposable
        {
            private readonly SqlCommand command;
            private readonly SqlConnection connection;
            private readonly int numberOfAsyncArguments;

            private ManualResetEvent mre;

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteReaderWrapper(string connectionString, string commandText, ManualResetEvent mre, int numberOfAsyncArgs)
            {
                this.numberOfAsyncArguments = numberOfAsyncArgs;
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
                    switch (this.numberOfAsyncArguments)
                    {
                        case 0:
                            {
                                var result = this.command.BeginExecuteReader();
                                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(15000));
                                this.command.EndExecuteReader(result);
                                break;
                            }
                        case 1:
                            { 
                                var result = this.command.BeginExecuteReader(CommandBehavior.SequentialAccess);
                                result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(15000));
                                this.command.EndExecuteReader(result);
                                break;
                            }
                        case 2:
                            {
                                this.command.BeginExecuteReader(Callback, null);
                                break;
                            }
                        case 3:
                            {
                                this.command.BeginExecuteReader(Callback, null, CommandBehavior.SequentialAccess);
                                break;
                            }
                        default:
                            {
                                throw new NotSupportedException("Not supported override");
                            }
                    }
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
                try
                {
                    this.command.EndExecuteReader(ar);
                }
                finally
                {
                    this.mre.Set();

                    if (connection != null)
                    {
                        this.connection.Close();
                    }
                }
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
                try
                {
                    this.command.EndExecuteReader(ar2);
                }
                finally
                {
                    this.mre.Set();
                    this.connection.Close();
                }
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
                try
                {
                    this.command.EndExecuteReader(ar2);
                }
                finally
                {
                    this.mre.Set();
                    this.connection.Close();
                }
            }
        }

        private sealed class AsyncExecuteNonQueryWrapper : IDisposable
        {
            private readonly SqlCommand command;
            private readonly int numberOfArgs;
            private readonly SqlConnection connection;

            private ManualResetEvent mre;
            
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
            public AsyncExecuteNonQueryWrapper(string connectionString, string commandText, ManualResetEvent mre, int numberOfArgs)
            {
                this.numberOfArgs = numberOfArgs;
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
                    switch (this.numberOfArgs)
                    {
                        case 0:
                            var result = this.command.BeginExecuteNonQuery();
                            result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(15000));
                            this.command.EndExecuteNonQuery(result);
                            break;
                        case 2:
                            this.command.BeginExecuteNonQuery(Callback, null);
                            break;
                        default:
                            throw new NotSupportedException("Override is not supported");
                    }
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
                try
                {
                    this.command.EndExecuteNonQuery(ar);
                }
                finally
                {
                    this.mre.Set();
                    this.connection.Close();
                }
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
                try
                {
                    this.command.EndExecuteXmlReader(ar);
                }
                finally
                {
                    this.mre.Set();
                    this.connection.Close();
                }
            }          
        }
#endif
    }
}