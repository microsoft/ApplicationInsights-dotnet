namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;

    internal class ApplicationFolderProvider : IApplicationFolderProvider
    {
        private readonly IDictionary environment;

        public ApplicationFolderProvider() : this(Environment.GetEnvironmentVariables())
        {
        }

        internal ApplicationFolderProvider(IDictionary environment)
        {
            this.environment = environment;
        }

        public IPlatformFolder GetApplicationFolder()
        {
            foreach (string rootPath in new[] { this.environment["LOCALAPPDATA"], this.environment["TEMP"] })
            {
                try
                {
                    if (!string.IsNullOrEmpty(rootPath))
                    {
                        var rootDirectory = new DirectoryInfo(rootPath);
                        DirectoryInfo telemetryDirectory = CreateTelemetrySubdirectory(rootDirectory);
                        CheckAccessPermissions(telemetryDirectory);
                        TelemetryChannelEventSource.Log.StorageFolder(telemetryDirectory.FullName);
                        return new PlatformFolder(telemetryDirectory);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedError();
            return null;
        }

        /// <summary>
        /// Throws <see cref="UnauthorizedAccessException" /> if the process lacks the required permissions to access the <paramref name="telemetryDirectory"/>.
        /// </summary>
        private static void CheckAccessPermissions(DirectoryInfo telemetryDirectory)
        {
            string testFileName = Path.GetRandomFileName();
            string testFilePath = Path.Combine(telemetryDirectory.FullName, testFileName);

            // FileSystemRights.CreateFiles
            using (var testFile = new FileStream(testFilePath, FileMode.CreateNew, FileAccess.ReadWrite))
            {
                // FileSystemRights.Write
                testFile.Write(new[] { default(byte) }, 0, 1);
            }

            // FileSystemRights.ListDirectory and FileSystemRights.Read 
            telemetryDirectory.GetFiles(testFileName);

            // FileSystemRights.DeleteSubdirectoriesAndFiles
            File.Delete(testFilePath);
        }

        private static DirectoryInfo CreateTelemetrySubdirectory(DirectoryInfo root)
        {
            string subdirectoryName = GetSHA1Hash(GetApplicationIdentity());
            string subdirectoryPath = Path.Combine(@"Microsoft\ApplicationInsights", subdirectoryName);
            return root.CreateSubdirectory(subdirectoryPath);
        }

        private static string GetApplicationIdentity()
        {
            return WindowsIdentity.GetCurrent().Name + "@" +
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Process.GetCurrentProcess().ProcessName);
        }

        private static string GetSHA1Hash(string input)
        {
            byte[] inputBits = Encoding.Unicode.GetBytes(input);
            byte[] hashBits = new SHA1CryptoServiceProvider().ComputeHash(inputBits);
            var hashString = new StringBuilder();
            foreach (byte b in hashBits)
            {
                hashString.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return hashString.ToString();
        }
    }
}
