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
        internal const string ConnectionString = @"Server =172.20.46.25;User Id = sa; Password=MSDNm4g4z!n4";

        public static bool CheckDatabaseExists(string databaseName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, ConnectionString, "master");
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

        public static void CreateDatabase(string databaseName, string databaseFileName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, ConnectionString, "master");
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string commandStr = $"CREATE DATABASE {databaseName} ON (NAME = N'{databaseName}', FILENAME = '{databaseFileName}')";
                SqlCommand cmd = new SqlCommand(commandStr, connection);

                //cmd.Parameters.Add(new SqlParameter("@databaseName", databaseName));
                //cmd.Parameters.Add(new SqlParameter("@databaseFileName", databaseFileName));
                cmd.ExecuteNonQuery();
            }
        }

        public static void ExecuteScript(string databaseName, string scriptName)
        {
            string connectionString = string.Format(CultureInfo.InvariantCulture, ConnectionString, databaseName);            
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
