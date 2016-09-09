namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;

    internal class ApplicationFolderProvider : IApplicationFolderProvider
    {
        private readonly IDictionary environment;
        private readonly string customFolderName;

        public ApplicationFolderProvider(string folderName = null)
            : this(Environment.GetEnvironmentVariables(), folderName)
        {
        }

        internal ApplicationFolderProvider(IDictionary environment, string folderName = null)
        {
            if (environment == null)
            {
                throw new ArgumentNullException("environment");
            }

            this.environment = environment;
            this.customFolderName = folderName;
        }

        public IPlatformFolder GetApplicationFolder()
        {
            var errors = new List<string>(this.environment.Count + 1);

            var result = CreateAndValidateApplicationFolder(this.customFolderName, false, errors);

            if (result == null)
            {
                foreach (string rootPath in new[] { this.environment["LOCALAPPDATA"], this.environment["TEMP"] })
                {
                    result = CreateAndValidateApplicationFolder(rootPath, true, errors);
                    if (result != null)
                    {
                        break;
                    }
                }
            }

            if (result == null)
            {
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedError(string.Join(Environment.NewLine, errors));
            }

            return result;
        }

        private static IPlatformFolder CreateAndValidateApplicationFolder(string rootPath, bool createSubFolder, IList<string> errors)
        {
            string errorMessage = null;
            IPlatformFolder result = null;

            try
            {
                if (!string.IsNullOrEmpty(rootPath))
                {
                    var telemetryDirectory = new DirectoryInfo(rootPath);
                    if (createSubFolder)
                    {
                        telemetryDirectory = CreateTelemetrySubdirectory(telemetryDirectory);
                    }

                    CheckAccessPermissions(telemetryDirectory);
                    TelemetryChannelEventSource.Log.StorageFolder(telemetryDirectory.FullName);

                    result = new PlatformFolder(telemetryDirectory);
                }
            }
            catch (UnauthorizedAccessException exp)
            {
                errorMessage = GetPathAccessFailureErrorMessage(exp, rootPath);
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedWarning(errorMessage);
            }
            catch (ArgumentException exp)
            {
                // Path does not specify a valid file path or contains invalid DirectoryInfo characters.
                errorMessage = GetPathAccessFailureErrorMessage(exp, rootPath);
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedWarning(errorMessage);
            }
            catch (DirectoryNotFoundException exp)
            {
                // The specified path is invalid, such as being on an unmapped drive.
                errorMessage = GetPathAccessFailureErrorMessage(exp, rootPath);
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedWarning(errorMessage);
            }
            catch (IOException exp)
            {
                // The subdirectory cannot be created. -or- A file or directory already has the name specified by path. -or-  The specified path, file name, or both exceed the system-defined maximum length. .
                errorMessage = GetPathAccessFailureErrorMessage(exp, rootPath);
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedWarning(errorMessage);
            }
            catch (SecurityException exp)
            {
                // The caller does not have code access permission to create the directory.
                errorMessage = GetPathAccessFailureErrorMessage(exp, rootPath);
                TelemetryChannelEventSource.Log.TransmissionStorageAccessDeniedWarning(errorMessage);
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                errors.Add(errorMessage);
            }

            return result;
        }

        private static string GetPathAccessFailureErrorMessage(Exception exp, string path)
        {
            return "Path: " + path + "; Error: " + exp.Message + Environment.NewLine;
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
            string subdirectoryName = GetSHA256Hash(GetApplicationIdentity());
            string subdirectoryPath = Path.Combine(@"Microsoft\ApplicationInsights", subdirectoryName);
            return root.CreateSubdirectory(subdirectoryPath);
        }

        private static string GetApplicationIdentity()
        {
            return WindowsIdentity.GetCurrent().Name + "@" +
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Process.GetCurrentProcess().ProcessName);
        }

        private static string GetSHA256Hash(string input)
        {
            byte[] inputBits = Encoding.Unicode.GetBytes(input);
            byte[] hashBits = new SHA256CryptoServiceProvider().ComputeHash(inputBits);
            var hashString = new StringBuilder();
            foreach (byte b in hashBits)
            {
                hashString.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }

            return hashString.ToString();
        }
    }
}
