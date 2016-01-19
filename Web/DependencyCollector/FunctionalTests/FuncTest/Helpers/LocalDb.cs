namespace FuncTest.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public class LocalDb
    {
        public const string LocalDbConnectionString = @"Data Source=.\SQLEXPRESS;Initial Catalog={0};Integrated Security=True;Connection Timeout=300";

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

        private static void ExecuteScript(string databaseName, string scriptName)
        {
            string connectionString = string.Format(LocalDbConnectionString, databaseName);

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
            string connectionString = string.Format(LocalDbConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = connection.CreateCommand();

                cmd.CommandText = string.Format("SELECT name FROM master.dbo.sysdatabases WHERE ('[' + name + ']' = '{0}' OR name = '{1}')", databaseName, databaseName);
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
            string connectionString = string.Format(LocalDbConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = connection.CreateCommand();

                cmd.CommandText = string.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", databaseName, databaseFileName);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
