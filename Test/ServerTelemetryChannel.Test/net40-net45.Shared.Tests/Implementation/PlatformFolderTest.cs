namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Helpers;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    

    /// <summary>
    /// We use these tests to understand actual behavior of <see cref="DirectoryInfo"/> and ensure that 
    /// <see cref="PlatformFolder"/> behaves consistently on all platforms we support.
    /// </summary>
    [TestClass]
    public class PlatformFolderTest : FileSystemTest, IDisposable
    {
        private DirectoryInfo storageFolder;

        public PlatformFolderTest()
        {
            string uniqueName = GetUniqueFileName();
            this.storageFolder = FileSystemTest.CreatePlatformFolder(uniqueName);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing == true)
            {
                try
                {
                    FileSystemTest.DeletePlatformItem(this.storageFolder);
                }
                catch (COMException)
                {
                    // WinRT exception if Folder is already deleted
                }
                catch (IOException)
                {
                    // !WinRT exception if Folder is already deleted
                }
            }
        }

        [TestClass]
        public class Class : PlatformFolderTest
        {
            [TestMethod]
            public void ConstructorThrowsArgumentNullExceptionWhenGivenFolderIsNullToPreventUsageErrors()
            {
                Assert.Throws<ArgumentNullException>(() => new PlatformFolder(null));
            }

            [TestMethod]
            public void ImplementsIFileSystemFolderInterfaceExpectedByIPlatform()
            {
                Assert.IsTrue(typeof(IPlatformFolder).IsAssignableFrom(typeof(PlatformFolder)));
            }
        }

        [TestClass]
        public class GetFilesAsync : PlatformFolderTest
        {
            [TestMethod]
            public void ReturnsEmptyEnumerableWhenStorageFolderIsEmpty()
            {
                var folder = new PlatformFolder(this.storageFolder);
                IEnumerable<IPlatformFile> files = folder.GetFiles();
                Assert.IsNotNull(files);
                Assert.Empty(files);
            }

            [TestMethod]
            public void ReturnsEmptyEnumerableWhenStorageFolderWasDeleted()
            {
                var folder = new PlatformFolder(this.storageFolder);
                FileSystemTest.DeletePlatformItem(this.storageFolder);
                IEnumerable<IPlatformFile> files = folder.GetFiles();
                Assert.IsNotNull(files);
                Assert.Empty(files);
            }

            [TestMethod]
            public void ReturnsEnumerableOfObjectsRepresentingExistingFiles()
            {
                string[] expectedFileNames = new string[] { "file.1", "blah.txt", "foo.bar" };
                foreach (string fileName in expectedFileNames)
                {
                    FileSystemTest.CreatePlatformFile(fileName, this.storageFolder);
                }

                var folder = new PlatformFolder(this.storageFolder);

                IEnumerable<IPlatformFile> files = folder.GetFiles();
                Assert.AreEqual(expectedFileNames.OrderBy(name => name), files.Select(f => f.Name).OrderBy(name => name));
            }

            [TestMethod]
            public void ThrowsUnauthorizedAccessExceptionWhenProcessDoesNotHaveRightToListDirectory()
            {
                using (new DirectoryAccessDenier(this.storageFolder, FileSystemRights.ListDirectory))
                {
                    var folder = new PlatformFolder(this.storageFolder);
                    Assert.Throws<UnauthorizedAccessException>(() => folder.GetFiles());
                }
            }
        }

        [TestClass]
        public class CreateFilesAsync : PlatformFolderTest
        {
            [TestMethod]
            public void CreatesFileWithSpecifiedName()
            {
                var folder = new PlatformFolder(this.storageFolder);

                string fileName = GetUniqueFileName();
                IPlatformFile file = folder.CreateFile(fileName);

                Assert.AreEqual(fileName, file.Name);
            }

            [TestMethod]
            public void CreatesPhysicalFileInFileSystem()
            {
                var folder = new PlatformFolder(this.storageFolder);

                string fileName = GetUniqueFileName();
                IPlatformFile file = folder.CreateFile(fileName);

                var storageFile = FileSystemTest.GetPlatformFile(fileName, this.storageFolder);
                Assert.IsNotNull(storageFile);
            }

            [TestMethod]
            public void ThrowsIOExceptionWhenFileWithThatNameAlreadyExists()
            {
                string fileName = GetUniqueFileName();
                FileSystemTest.CreatePlatformFile(fileName, this.storageFolder);

                var folder = new PlatformFolder(this.storageFolder);
                Assert.Throws<IOException>(() => folder.CreateFile(fileName));
            }

            [TestMethod]
            public void ThrowsArgumentNullExceptionWhenGivenFileNameIsNull()
            {
                var folder = new PlatformFolder(this.storageFolder);
                Assert.Throws<ArgumentNullException>(() => folder.CreateFile(null));
            }

            [TestMethod]
            public void ThrowsArgumentExceptionWhenDesiredFileNameIsEmpty()
            {
                var folder = new PlatformFolder(this.storageFolder);
                Assert.Throws<ArgumentException>(() => folder.CreateFile(string.Empty));
            }

            [TestMethod]
            public void ThrowsIOExceptionWhenDesiredFileNameIsTooLong()
            {
                var folder = new PlatformFolder(this.storageFolder);
                Assert.Throws<PathTooLongException>(() => folder.CreateFile(new string('F', 1024)));
            }

            [TestMethod]
            public void ThrowsUnauthorizedAccessExceptionWhenProcessDoesNotHaveRightToCreateFile()
            {
                using (new DirectoryAccessDenier(this.storageFolder, FileSystemRights.CreateFiles))
                { 
                    var folder = new PlatformFolder(this.storageFolder);
                    Assert.Throws<UnauthorizedAccessException>(() => folder.CreateFile(FileSystemTest.GetUniqueFileName()));
                }
            }

            [TestMethod]
            public void RecreatesFolderIfItWasDeleted()
            {
                var folder = new PlatformFolder(this.storageFolder);
                FileSystemTest.DeletePlatformItem(this.storageFolder);
                string fileName = GetUniqueFileName();
                folder.CreateFile(fileName);
                Assert.IsNotNull(FileSystemTest.GetPlatformFile(fileName, this.storageFolder));
            }
        }

        [TestClass]
        public class DeleteAsync : PlatformFolderTest
        {
            [TestMethod]
            public void DeletesFolderFromFileSystem()
            {
                IPlatformFolder folder = new PlatformFolder(this.storageFolder);

                folder.Delete();

                Assert.IsFalse(folder.Exists());
            }

            [TestMethod]
            public void ThrowsIOExceptionWhenFileIsAlreadyDeleted()
            {
                IPlatformFolder folder = new PlatformFolder(this.storageFolder);
                FileSystemTest.DeletePlatformItem(this.storageFolder);

                Assert.Throws<DirectoryNotFoundException>(() => folder.Delete());
            }
        }

        [TestClass]
        public class ExistsAsync : PlatformFolderTest
        {
            [TestMethod]
            public void ReturnsTrueWhenFolderExists()
            {
                IPlatformFolder folder = new PlatformFolder(this.storageFolder);

                bool folderExists = folder.Exists();

                Assert.IsTrue(folderExists);
            }

            [TestMethod]
            public void ReturnsFalseWhenFolderDoesNotExist()
            {
                FileSystemTest.DeletePlatformItem(this.storageFolder);
                IPlatformFolder folder = new PlatformFolder(this.storageFolder);

                bool folderExists = folder.Exists();

                Assert.IsFalse(folderExists);
            }
        }
    }
}