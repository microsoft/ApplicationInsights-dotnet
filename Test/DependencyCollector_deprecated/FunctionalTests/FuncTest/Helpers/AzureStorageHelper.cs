using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace FuncTest.Helpers
{
    public static class AzureStorageHelper
    {
        public const string AzureEmulatorPath = @"\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe";
        public static void Initialize()
        {
            StartEmulator();

            Seed();
        }

        private static bool IsEmulatorRunning()
        {
            string output = ExecuteEmulatorCommand("status");

            if (output.Contains("IsRunning: False"))
            {
                return false;
            }
            else if (output.Contains("IsRunning: True"))
            {
                return true;
            }
            else
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "Failed to get status of Azure Storage Emulator: {0}", output));
            }
        }

        private static string ExecuteEmulatorCommand(string command)
        {
            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") + AzureEmulatorPath;
            p.StartInfo.Arguments = command;
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, "Failed to execute command {0} on Azure Storage Emulator: {1}", command, output));
            }
            

            return output;
        }

        private static void StartEmulator()
        {
            if (!IsEmulatorRunning())
            {
                ExecuteEmulatorCommand("start");
            }
        }

        public static void Cleanup()
        {
            if (IsEmulatorRunning())
            {
                ExecuteEmulatorCommand("clear");
                ExecuteEmulatorCommand("stop");
            }
        }

        public static void Seed()
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("rddtest");

            container.CreateIfNotExists();

            // Retrieve reference to a blob named "testblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("testblob");

            if (!blockBlob.Exists())
            {
                var stream = new MemoryStream();
                try
                {
                    var writer = new StreamWriter(stream, new UnicodeEncoding());
                    writer.Write("test content");
                    writer.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    blockBlob.UploadFromStream(stream);
                }
                finally
                {
                    stream.Dispose();
                }
            }
        }
    }
}
