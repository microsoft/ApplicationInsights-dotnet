namespace FuncTest.Helpers
{
    using System;
    using System.Data.SqlClient;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    public class LocalDb
    {
        public const string LocalDbConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog={0};Integrated Security=True;Connection Timeout=300";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:Pass System.Uri objects instead of strings", Justification = "Not a normal URI.")]
        public static void CreateLocalDb(string databaseName, string scriptName)
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            string outputFolder = Path.Combine(Environment.GetEnvironmentVariable("SystemDrive") + "\\", "SqlExpress");
            string mdfFilename = databaseName + ".mdf";
            string databaseFileName = Path.Combine(outputFolder, mdfFilename);

            // Create Data Directory If It Doesn't Already Exist.
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            else if (!CheckDatabaseExists(databaseName))
            {
                // If the database does not already exist, create it.
                CreateDatabase(databaseName, databaseFileName);
                ExecuteScript(databaseName, scriptName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities")]
        private static void ExecuteScript(string databaseName, string scriptName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, LocalDbConnectionString, databaseName);

            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);

            var file = new FileInfo(scriptName);
            string script = file.OpenText().ReadToEnd();

            string[] commands = script.Split(new[] { "GO\r\n", "GO ", "GO\t" }, StringSplitOptions.RemoveEmptyEntries);

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (string c in commands)
                {
                    var command = new SqlCommand(c, connection);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static bool CheckDatabaseExists(string databaseName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, LocalDbConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(
                    "SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '@databaseName' OR name = '@databaseName')",
                    connection);

                cmd.Parameters.Add(new SqlParameter("@databaseName", databaseName));
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static void CreateDatabase(string databaseName, string databaseFileName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, LocalDbConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("CREATE DATABASE {0} ON (NAME = N'@databaseName', FILENAME = '@databaseFileName')");

                cmd.Parameters.Add(new SqlParameter("@databaseName", databaseName));
                cmd.Parameters.Add(new SqlParameter("@databaseFileName", databaseFileName));
                cmd.ExecuteNonQuery();
            }
        }
    }
}
