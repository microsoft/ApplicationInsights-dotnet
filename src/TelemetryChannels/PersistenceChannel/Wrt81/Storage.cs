// <copyright file="Storage.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Windows.Storage;
    using Windows.Storage.FileProperties;
    using Windows.Storage.Search;

    internal class Storage : StorageBase
    {
        private readonly FixedSizeQueue<string> deletedFilesQueue;
        private Task calculateSizeTask;
        private long storageSize = 0;
        private long storageCountFiles = 0;
        private object peekLockObj = new object();
        private StorageFolder storageFolder;
        private object storageFolderLock = new object();
        private bool storageFolderInitialized = false;
        private uint transmissionsDropped = 0;
        private string storageFolderName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Storage"/> class.
        /// </summary>
        /// <param name="uniqueFolderName">A folder name. Under this folder all the transmissions will be saved.</param>
        internal Storage(string uniqueFolderName)
        {
            this.peekedTransmissions = new SnapshottingDictionary<string, string>();
            this.storageFolderName = uniqueFolderName;
            this.deletedFilesQueue = new FixedSizeQueue<string>(10);
            if (string.IsNullOrEmpty(uniqueFolderName))
            {
                this.storageFolderName = "ApplicationInsights";
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
        private StorageFolder StorageFolder
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
                                this.storageFolder = ApplicationData
                                                .Current
                                                .LocalFolder
                                                .CreateFolderAsync(this.FolderName, CreationCollisionOption.OpenIfExists)
                                                .AsTask()
                                                .ConfigureAwait(false)
                                                .GetAwaiter()
                                                .GetResult();
                            }
                            catch (Exception e)
                            {   
                                this.storageFolder = null;
                                string error = string.Format("Failed to create storage folder: {0}", e);
                                CoreEventSource.Log.LogVerbose(error);
                            }

                            this.storageFolderInitialized = true;
                            string msg = string.Format("Storage folder: {0}", this.storageFolder == null ? "null" : this.storageFolder.Path);
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
            IEnumerable<StorageFile> files = this.GetFiles(CommonFileQuery.OrderByName, ".trn", top: 50);

            lock (this.peekLockObj)
            {
                foreach (IStorageFile file in files)
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
                            this.peekedTransmissions.Add(file.Name, storageTransmissionItem.FullFilePath);
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
            try
            {
                if (this.StorageFolder == null)
                {
                    return;
                }

                // Initial storage size calculation. 
                this.EnsureSizeIsCalculatedAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                IStorageFile file = this.StorageFolder.GetFileAsync(item.FileName).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                long fileSize = this.GetSizeAsync(file).ConfigureAwait(false).GetAwaiter().GetResult();
                file.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                this.deletedFilesQueue.Enqueue(item.FileName);

                // calculate size                
                Interlocked.Add(ref this.storageSize, -fileSize);
                Interlocked.Decrement(ref this.storageCountFiles);
            }
            catch (IOException e)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Failed to delete a file. file: {0} Exception: {1}", item == null ? "null" : item.FullFilePath, e);
                CoreEventSource.Log.LogVerbose(msg);
            }
        }

        internal override async Task EnqueueAsync(Transmission transmission)
        {
            try
            {   
                if (transmission == null || this.StorageFolder == null)
                {
                    return;
                }

                // Initial storage size calculation. 
                await this.EnsureSizeIsCalculatedAsync().ConfigureAwait(false);

                if ((ulong)this.storageSize >= this.CapacityInBytes || this.storageCountFiles >= this.MaxFiles)
                {
                    // if max storage capacity has reached, drop the transmission (but log every 100 lost transmissions). 
                    if (this.transmissionsDropped++ % 100 == 0)
                    {
                        CoreEventSource.Log.LogVerbose("Total transmissions dropped: " + this.transmissionsDropped);
                    }

                    return;
                }

                // Writes content to a temporaty file and only then rename to avoid the Peek from reading the file before it is being written.
                // Creates the temp file name
                string tempFileName = Guid.NewGuid().ToString("N");                

                // Creates the temp file (doesn't save any content. Just creates the file)
                IStorageFile temporaryFile = await this.StorageFolder.CreateFileAsync(tempFileName + ".tmp").AsTask().ConfigureAwait(false);

                // Now that the file got created we can increase the files count
                Interlocked.Increment(ref this.storageCountFiles);

                // Saves transmission to the temp file
                await SaveTransmissionToFileAsync(transmission, temporaryFile).ConfigureAwait(false);

                // Now that the file is written increase storage size. 
                long temporaryFileSize = await this.GetSizeAsync(temporaryFile).ConfigureAwait(false);
                Interlocked.Add(ref this.storageSize, temporaryFileSize);

                // Creates a new file name
                string now = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                string newFileName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.trn", now, tempFileName);

                // Renames the file
                await temporaryFile.RenameAsync(newFileName, NameCollisionOption.FailIfExists).AsTask().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                CoreEventSource.Log.LogVerbose(string.Format(CultureInfo.InvariantCulture, "EnqueueAsync: Exception: {0}", e));
            }
        }

        private static async Task SaveTransmissionToFileAsync(Transmission transmission, IStorageFile file)
        {
            try
            {
                using (Stream stream = await file.OpenStreamForWriteAsync().ConfigureAwait(false))
                {
                    await StorageTransmission.SaveAsync(transmission, stream).ConfigureAwait(false);
                }
            }
            catch (UnauthorizedAccessException)
            {
                string message = string.Format("Failed to save transmission to file. UnauthorizedAccessException. File path: {0}, FileName: {1}", file.Path, file.Name);
                CoreEventSource.Log.LogVerbose(message);
                throw;
            }
        }

        private static async Task<StorageTransmission> LoadTransmissionFromFileAsync(IStorageFile file)
        {
            try
            {
                using (Stream stream = await file.OpenStreamForReadAsync().ConfigureAwait(false))
                {
                    StorageTransmission storageTransmissionItem = await StorageTransmission.CreateFromStreamAsync(stream, file.Path).ConfigureAwait(false);
                    return storageTransmissionItem;
                }
            }
            catch (Exception e)
            {
                string message = string.Format("Failed to load transmission from file. File path: {0}, FileName: {1}, Exception: {2}", file.Path, file.Name, e);
                CoreEventSource.Log.LogVerbose(message);
                throw;
            }
        }

        private async Task EnsureSizeIsCalculatedAsync()
        {
            await LazyInitializer.EnsureInitialized(ref this.calculateSizeTask, this.CalculateSizeAsync).ConfigureAwait(false);
        }

        /// <summary>
        /// Get files from <see cref="storageFolder"/>.
        /// </summary>
        /// <param name="fileQuery">Define the logic for sorting the files.</param>
        /// <param name="filterByExtension">Defines a file extension. This method will return only files with this extension.</param>
        /// <param name="top">Define how many files to return. This can be useful when the directory has a lot of files, in that case 
        /// GetFilesAsync will have a performance hit.</param>
        private IEnumerable<StorageFile> GetFiles(CommonFileQuery fileQuery, string filterByExtension, uint top)
        {
            IEnumerable<StorageFile> files = new List<StorageFile>();

            try
            {
                if (this.StorageFolder != null)
                {
                    files = this.StorageFolder
                                .GetFilesAsync(CommonFileQuery.DefaultQuery, 0, top)
                                .AsTask()
                                .ConfigureAwait(false)
                                .GetAwaiter()
                                .GetResult();

                    // a low 'top' value might cause a bug if there are more then 50 tmp files. This is a trade off, 
                    // because reading all the files (no top) has a performance hit and there is no expectation to have 50 tmp files. 
                    return files.Where((file) => Path.GetExtension(file.Name).Equals(filterByExtension, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch (Exception e)
            {
                string msg = string.Format(CultureInfo.InvariantCulture, "Peek failed while get files from storage. Exception: " + e);
                CoreEventSource.Log.LogVerbose(msg);
            }

            return files;
        }

        /// <summary>
        /// Gets a file's size.
        /// </summary>
        private async Task<long> GetSizeAsync(IStorageFile file)
        {
            BasicProperties fileProperties = await file.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false);
            return (long)fileProperties.Size;
        }

        /// <summary>
        /// Check the storage limits and return true if they reached. 
        /// Storage limits are defined by the number of files and the total size on disk. 
        /// </summary>        
        private async Task CalculateSizeAsync()
        {
            IReadOnlyList<IStorageFile> storageFiles = await this.StorageFolder.GetFilesAsync().AsTask().ConfigureAwait(false);
            this.storageCountFiles = (long)storageFiles.Count;

            long storageSizeInBytes = 0;
            foreach (StorageFile file in storageFiles)
            {
                BasicProperties basic = file.GetBasicPropertiesAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
                storageSizeInBytes += (long)basic.Size;
            }

            this.storageSize = storageSizeInBytes;
        }

        /// <summary>
        /// Enqueue is saving a transmission to a <c>tmp</c> file and after a successful write operation it renames it to a <c>trn</c> file. 
        /// A file without a <c>trn</c> extension is ignored by Storage.Peek(), so if a process is taken down before rename happens 
        /// it will stay on the disk forever. 
        /// This thread deletes files with the <c>tmp</c> extension that exists on disk for more than 5 minutes.
        /// </summary>
        private void DeleteObsoleteFiles()
        {
            try
            {
                IEnumerable<StorageFile> files = this.GetFiles(CommonFileQuery.DefaultQuery, ".tmp", 50);
                foreach (StorageFile file in files)
                {
                    // if the file is older then a minute - delete it.
                    if (DateTime.UtcNow - file.DateCreated.UtcDateTime >= TimeSpan.FromMinutes(5))
                    {
                        file.DeleteAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
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
