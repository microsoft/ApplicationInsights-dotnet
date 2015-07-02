namespace Microsoft.ApplicationInsights.PersistenceChannel.Net40.Tests
{
    using System;
    using System.IO;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.PersistenceChannel.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Run storage tests. 
    /// <seealso cref="StorageTestsBase"/>.
    /// </summary>
    [TestClass]
    public class StorageTests : StorageTestsBase
    {
        /// <summary>
        /// Every unit test creates a storage folder by this name <c>unittest[Guid]</c>. 
        /// This cleanup deletes all those folders after the test finishes. 
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            string basePath = Path.GetDirectoryName(storage.StorageFolder.FullName);
            foreach (string folder in Directory.EnumerateDirectories(basePath, "unittest*"))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
