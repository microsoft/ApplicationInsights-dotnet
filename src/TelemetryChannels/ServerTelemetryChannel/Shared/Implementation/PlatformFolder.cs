namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal class PlatformFolder : IPlatformFolder
    {
        private readonly DirectoryInfo folder;

        public PlatformFolder(DirectoryInfo folder)
        {
            if (folder == null)
            {
                throw new ArgumentNullException("folder");
            }

            this.folder = folder;
        }

        public string Name
        {
            get { return this.folder.Name; }
        }

        internal DirectoryInfo Folder
        {
            get { return this.folder; }
        }

        public bool Exists()
        {
            return Directory.Exists(this.folder.FullName);
        }

        public void Delete()
        {
            this.folder.Delete();
        }

        public IEnumerable<IPlatformFile> GetFiles()
        {
            try
            {
                return this.folder.GetFiles().Select(fileInfo => new PlatformFile(fileInfo));
            }
            catch (DirectoryNotFoundException)
            {
                // Return empty list for compatibility with Windows runtime.
                return Enumerable.Empty<IPlatformFile>();
            }
        }

        public IPlatformFile CreateFile(string fileName)
        {
            // Check argument manually for consistent behavior on both Silverlight and Windows runtimes
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("fileName");
            }

            this.folder.Create();
            var fileInfo = new FileInfo(Path.Combine(this.folder.FullName, fileName));
            if (fileInfo.Exists)
            {
                throw new IOException("Cannot create file '" + fileName + "' because it already exists.");
            }

            using (fileInfo.Create())
            {
            }

            return new PlatformFile(fileInfo);
        }
    }
} 