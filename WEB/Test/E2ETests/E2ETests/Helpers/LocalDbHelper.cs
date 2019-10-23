using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E2ETests.Helpers
{
    public class LocalDbHelper
    {
        internal const string ConnectionStringFormat = @"Server ={0};User Id = sa; Password=MSDNm4g4z!n4"; // [SuppressMessage("Microsoft.Security", "CS001:SecretInLine", Justification="Database Password for Docker container.")]
        string ConnectionString;

        public LocalDbHelper(string serverIp)
        {
            this.ConnectionString = string.Format(ConnectionStringFormat, serverIp);
        }

        public bool CheckDatabaseExists(string databaseName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, this.ConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select db_id(@databaseName)", connection);

                cmd.Parameters.Add(new SqlParameter("@databaseName", databaseName));
                object result = cmd.ExecuteScalar();
                if (result != null && !Convert.IsDBNull(result))
                {
                    return true;
                }
            }
            return false;
        }

        public void CreateDatabase(string databaseName, string databaseFileName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, this.ConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string commandStr = $"CREATE DATABASE {databaseName} ON (NAME = N'{databaseName}', FILENAME = '{databaseFileName}')";
                SqlCommand cmd = new SqlCommand(commandStr, connection);
                cmd.ExecuteNonQuery();
            }
        }

        public void ExecuteScript(string databaseName, string scriptName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, this.ConnectionString, databaseName);            
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
    }
}
