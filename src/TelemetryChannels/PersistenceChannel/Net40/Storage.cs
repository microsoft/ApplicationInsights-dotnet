// <copyright file="Storage.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class Storage : StorageBase
    {
        private readonly FixedSizeQueue<string> deletedFilesQueue;
        private object peekLockObj = new object();
        private DirectoryInfo storageFolder;
        private int transmissionsDropped = 0;
        private string storageFolderName;
        private bool storageFolderInitialized;
        private object storageFolderLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="Storage"/> class.
        /// </summary>
        /// <param name="uniqueFolderName">A folder name. Under this folder all the transmissions will be saved.</param>
        internal Storage(string uniqueFolderName)
        {
            this.peekedTransmissions = new ConcurrentDictionary<string, string>();
            this.deletedFilesQueue = new FixedSizeQueue<string>(10);
            this.storageFolderName = uniqueFolderName;
            if (string.IsNullOrEmpty(uniqueFolderName))
            {
                string appId = GetApplicationIdentity();
                this.storageFolderName = GetSHA1Hash(appId);
            }

            this.CapacityInBytes = 10 * 1024 * 1024; // 10 MB
            this.MaxFiles = 5000;

            Task.Factory.StartNew(this.DeleteObsoleteFiles)
                .ContinueWith(
                    task =>
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, "Storage: Unhandled exception in DeleteObsoleteFiles: {0}", task.Exception);
                        CoreEventSource.Log.LogVerbose(msg);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Gets the storage's folder name.
        /// </summary>
        internal override string FolderName
        {
            get { return this.storageFolderName; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether storage folder was already tried to be created. Only used for UTs. 
        /// Once this value is true, StorageFolder will always return null, which mocks scenario that storage's folder 
        /// couldn't be created.
        /// </summary>
        internal bool StorageFolderInitialized
        {
            get
            {
                return this.storageFolderInitialized;
            }

            set
            {
                this.storageFolderInitialized = value;
            }
        }

        /// <summary>
        /// Gets the storage folder. If storage folder couldn't be created, null will be returned.
        /// </summary>        
        internal DirectoryInfo StorageFolder
        {
            get
            {
                if (!this.storageFolderInitialized)
                {
                    lock (this.storageFolderLock)
                    {
                        if (!this.storageFolderInitialized)
                        {
                            try
                            {
                                this.storageFolder = this.GetApplicationFolder();
                            }
                            catch (Exception e)
                            {
                                this.storageFolder = null;
                                string error = string.Format("Failed to create storage folder: {0}", e);
                                CoreEventSource.Log.LogVerbose(error);
                            }

                            this.storageFolderInitialized = true;
                            string msg = string.Format("Storage folder: {0}", this.storageFolder == null ? "null" : this.storageFolder.FullName);
                            CoreEventSource.Log.LogVerbose(msg);
                        }
                    }
                }

                return this.storageFolder;
            }
        }

        /// <summary>
        /// Reads an item from the storage. Order is Last-In-First-Out. 
        /// When the Transmission is no longer needed (it was either sent or failed with a non-retriable error) it should be disposed. 
        /// </summary>
        internal override StorageTransmission Peek()
        {
            IEnumerable<FileInfo> files = this.GetFiles("*.trn");

            lock (this.peekLockObj)
            {
                foreach (FileInfo file in files)
                {
                    try
                    {
                        // if a file was peeked before, skip it (wait until it is disposed).  
                        if (this.peekedTransmissions.ContainsKey(file.Name) == false && this.deletedFilesQueue.Contains(file.Name) == false)
                        {
                            // Load the transmission from disk.
                            StorageTransmission storageTransmissionItem = LoadTransmissionFromFileAsync(file).ConfigureAwait(false).GetAwaiter().GetResult();

                            // when item is disposed it should be removed from the peeked list.
                            storageTransmissionItem.Disposing = item => this.OnPeekedItemDisposed(file.Name);

                            // add the transmission to the list.
                            this.peekedTransmissions.Add(file.Name, file.FullName);
                            return storageTransmissionItem;
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, "Failed to load an item from the storage. file: {0} Exception: {1}", file, e);
                        CoreEventSource.Log.LogVerbose(msg);
                    }
                }
            }

            return null;
        }

        internal override void Delete(StorageTransmission item)
        {
            if (this.StorageFolder == null)
            {
                return;
            }

            try
            {
                File.Delete(item.FullFilePath);
                this.deletedFilesQueue.Enqueue(item.FileName);
            }
            catch (IOException e)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Failed to delete a file. file: {0} Exception: {1}", item == null ? "null" : item.FullFilePath, e);
                CoreEventSource.Log.LogVerbose(msg);
            }
            catch (UnauthorizedAccessException e)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Failed to delete a file. file: {0} Exception: {1}", item == null ? "null" : item.FullFilePath, e);
                CoreEventSource.Log.LogVerbose(msg);
            }
        }

        internal override async Task EnqueueAsync(Transmission transmission)
        {   
            try
            {
                if (this.StorageFolder == null)
                {
                    return;
                }

                if (transmission == null)
                {
                    CoreEventSource.Log.LogVerbose("transmission is null. EnqueueAsync is skipped");
                    return;
                }

                if (this.IsStorageLimitsReached())
                {
                    // if max storage capacity has reached, drop the transmission (but log every 100 lost transmissions). 
                    if (Interlocked.Increment(ref this.transmissionsDropped) % 100 == 0)
                    {
                        CoreEventSource.Log.LogVerbose("Total transmissions dropped: " + this.transmissionsDropped);
                    }

                    return;
                }

                // Write content to a temporaty file and only then rename to avoid the Peek method from reading the file before it is being written.
                // Creates the temp file name
                string tempFileName = Guid.NewGuid().ToString("N");
                string tempFullFilePath = Path.Combine(this.StorageFolder.FullName, tempFileName + ".tmp");

                // Saves tranmission to file
                await SaveTransmissionToFileAsync(transmission, tempFullFilePath).ConfigureAwait(false);

                // Creates a new file name
                string now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                string newFileName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.trn", now, tempFileName);
                string newFillFilePath = Path.Combine(this.StorageFolder.FullName, newFileName);

                // Renames the file
                File.Move(tempFullFilePath, newFillFilePath);
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose(string.Format(CultureInfo.InvariantCulture, "EnqueueAsync: Exception: {0}", e));
            }
        }

        private static async Task SaveTransmissionToFileAsync(Transmission transmission, string fileFullName)
        {
            try
            {
                using (Stream stream = File.OpenWrite(fileFullName))
                {
                    await StorageTransmission.SaveAsync(transmission, stream).ConfigureAwait(false);
                }
            }
            catch (UnauthorizedAccessException)
            {
                string message = string.Format("Failed to save transmission to file. UnauthorizedAccessException. File full path: {0}", fileFullName);
                CoreEventSource.Log.LogVerbose(message);
                throw;
            }
        }

        private static async Task<StorageTransmission> LoadTransmissionFromFileAsync(FileInfo file)
        {
            try
            {
                using (Stream stream = file.OpenRead())
                {
                    StorageTransmission storageTransmissionItem = await StorageTransmission.CreateFromStreamAsync(stream, file.FullName).ConfigureAwait(false);
                    return storageTransmissionItem;
                }
            }
            catch (Exception e)
            {
                string message = string.Format("Failed to load transmission from file. File full path: {0}, Exception: {1}", file.FullName, e);
                CoreEventSource.Log.LogVerbose(message);
                throw;
            }
        }

        private static void CheckAccessPermissions(DirectoryInfo telemetryDirectory)
        {
            // This should throw UnauthorizedAccessException if the process lacks permissions to list and read files.
            // We don't check for write permissions to avoid additional disk access and rely on TransmissionStorage 
            // to handle the UnauthorizedAccessException gracefully.
            telemetryDirectory.GetFiles("_");
        }

        private static string GetApplicationIdentity()
        {
            // get user
            string user = string.Empty;
            try
            {
                user = WindowsIdentity.GetCurrent().Name;
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose(string.Format("GetApplicationIdentity: Failed to read user identity. Exception: {0}", e));
            }

            // get domain's directory
            string domainDirecotry = string.Empty;
            try
            {
                domainDirecotry = AppDomain.CurrentDomain.BaseDirectory;
            }
            catch (AppDomainUnloadedException e)
            {   
                CoreEventSource.Log.LogVerbose(string.Format("GetApplicationIdentity: Failed to read the domain's base directory. Exception: {0}", e));
            }

            // get process name
            string processName = string.Empty;
            try
            {
                processName = Process.GetCurrentProcess().ProcessName;
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose(string.Format("GetApplicationIdentity: Failed to read the process name. Exception: {0}", e));
            }

            string appId = string.Format("{0}@{1}{2}", user, domainDirecotry, processName);
            return appId;
        }

        private static string GetSHA1Hash(string input)
        {   
            byte[] inputBits = Encoding.Unicode.GetBytes(input);
            try
            {   
                byte[] hashBits = new SHA1CryptoServiceProvider().ComputeHash(inputBits);
                var hashString = new StringBuilder();
                foreach (byte b in hashBits)
                {
                    hashString.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }

                return hashString.ToString();
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose(string.Format("GetSHA1Hash('{0}'): Failed to hash. Change string to Base64. Exception: {1}", input, e));
                return "Storage";
            }
        }

        private DirectoryInfo GetApplicationFolder()
        {
            IDictionary environment = Environment.GetEnvironmentVariables();

            var folderOption1 = new { RootPath = environment["LOCALAPPDATA"] as string,   AISubFolder = @"Microsoft\ApplicationInsights" };
            var folderOption2 = new { RootPath = environment["TEMP"] as string,           AISubFolder = @"Microsoft\ApplicationInsights" };
            var folderOption3 = new { RootPath = environment["ProgramData"] as string,    AISubFolder = @"Microsoft ApplicationInsights" };

            foreach (var folderOption in new[] { folderOption1, folderOption2, folderOption3 })
            {
                try
                {
                    if (!string.IsNullOrEmpty(folderOption.RootPath))
                    {
                        var root = new DirectoryInfo(folderOption.RootPath);
                        string subdirectoryPath = Path.Combine(folderOption.AISubFolder, this.storageFolderName);
                        DirectoryInfo telemetryDirectory = root.CreateSubdirectory(subdirectoryPath);
                        CheckAccessPermissions(telemetryDirectory);
                        return telemetryDirectory;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    continue;
                }
            }

            return null;
        }

        private bool IsStorageLimitsReached()
        {
            if (this.MaxFiles == uint.MaxValue && this.CapacityInBytes == ulong.MaxValue)
            {
                return false;
            }

            FileInfo[] files = this.StorageFolder.GetFiles();
            if (files.Length >= this.MaxFiles)
            {
                return true;
            }

            ulong storageSizeInBytes = (ulong)files.Sum((fileInfo) => fileInfo.Length);
            return storageSizeInBytes >= this.CapacityInBytes;
        }

        /// <summary>
        /// Get files from <see cref="storageFolder"/>.
        /// </summary>        
        private IEnumerable<FileInfo> GetFiles(string filter)
        {
            IEnumerable<FileInfo> files = new List<FileInfo>();

            try
            {
                if (this.StorageFolder != null)
                {
                    files = this.StorageFolder.GetFiles(filter, SearchOption.TopDirectoryOnly);
                    return files.OrderBy(fileInfo => fileInfo.Name);
                }
            }
            catch (Exception e)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Peek failed while getting files from storage. Exception: " + e);
                CoreEventSource.Log.LogVerbose(msg);
            }

            return files;
        }

        /// <summary>
        /// Enqueue is saving a transmission to a <c>tmp</c> file and after a successful write operation it renames it to a <c>trn</c> file. 
        /// A file without a <c>trn</c> extension is ignored by Storage.Peek(), so if a process is taken down before rename happens 
        /// it will stay on the disk forever. 
        /// This method deletes files with the <c>tmp</c> extension that exists on disk for more than 5 minutes.
        /// </summary>
        private void DeleteObsoleteFiles()
        {
            try
            {
                IEnumerable<FileInfo> files = this.GetFiles("*.tmp");
                foreach (FileInfo file in files)
                {
                    // if the file is older then a minute - delete it.
                    if (DateTime.UtcNow - file.CreationTimeUtc >= TimeSpan.FromMinutes(5))
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose("Failed to delete tmp files. Exception: " + e);
            }
        }
    }
}
