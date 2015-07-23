namespace Microsoft.ApplicationInsights.PersistenceChannel.Tests
{
    using System;
    using System.Text;
#if !WINDOWS_PHONE && !WINDOWS_STORE
    using System.Threading.Tasks;
#endif
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;    
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;    
#endif

    /// <summary>
    /// Tests for Storage. 
    /// </summary>
    /// <remarks>
    /// To reduce complexity, there was a design decision to make Storage the file system abstraction layer. 
    /// That means that Storage knows about the file system types (e.g. IStorageFile or FileInfo). 
    /// Those types are not easy to mock (even IStorageFile is using extension methods that makes it very hard to mock).
    /// Therefore those UnitTests just doesn't mock the file system. Every unit test in <see cref="StorageTests"/>  
    /// reads and writes files to/from the disk. 
    /// </remarks>
    public class StorageTestsBase
    {
        [TestMethod]
        public void EnqueuedContentIsEqualToPeekedContent()
        {
            // Setup
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            Transmission transmissionToEnqueue = CreateTransmission(new TraceTelemetry("mock_item"));            

            // Act
            storage.EnqueueAsync(transmissionToEnqueue).ConfigureAwait(false).GetAwaiter().GetResult();
            StorageTransmission peekedTransmission = storage.Peek();

            // Asserts
            string enqueuedContent = Encoding.UTF8.GetString(transmissionToEnqueue.Content, 0, transmissionToEnqueue.Content.Length);
            string peekedContent = Encoding.UTF8.GetString(peekedTransmission.Content, 0, peekedTransmission.Content.Length);
            Assert.AreEqual(peekedContent, enqueuedContent);
        }

        [TestMethod]
        public void DeletedItemIsNotReturnedInCallsToPeek()
        {
            // Setup - create a storage with one item
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            Transmission transmissionToEnqueue = CreateTransmissionAndEnqueueIt(storage);

            // Act
            StorageTransmission firstPeekedTransmission;

            // if item is not disposed,peek will not return it (regardless of the call to delete). 
            // So for this test to actually test something, using 'using' is required.  
            using (firstPeekedTransmission = storage.Peek()) 
            {   
                storage.Delete(firstPeekedTransmission);
            }

            StorageTransmission secondPeekedTransmission = storage.Peek();

            // Asserts            
            Assert.IsNotNull(firstPeekedTransmission);
            Assert.IsNull(secondPeekedTransmission);
        }

        [TestMethod]
        public void PeekedItemIsNOnlyotReturnedOnce()
        {
            // Setup - create a storage with one item
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            Transmission transmissionToEnqueue = CreateTransmissionAndEnqueueIt(storage);            

            // Act
            StorageTransmission firstPeekedTransmission = storage.Peek();            
            StorageTransmission secondPeekedTransmission = storage.Peek();

            // Asserts            
            Assert.IsNotNull(firstPeekedTransmission);
            Assert.IsNull(secondPeekedTransmission);
        }

        [TestMethod]
        public void PeekedItemIsReturnedAgainAfterTheItemInTheFirstCallToPeekIsDisposed()
        {
            // Setup - create a storage with one item
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            Transmission transmissionToEnqueue = CreateTransmission(new TraceTelemetry("mock_item"));
            storage.EnqueueAsync(transmissionToEnqueue).ConfigureAwait(false).GetAwaiter().GetResult();

            // Act
            StorageTransmission firstPeekedTransmission;
            using (firstPeekedTransmission = storage.Peek())
            {
            }

            StorageTransmission secondPeekedTransmission = storage.Peek();

            // Asserts            
            Assert.IsNotNull(firstPeekedTransmission);
            Assert.IsNotNull(secondPeekedTransmission);
        }

        [TestMethod]
        public void WhenStorageHasTwoItemsThenTwoCallsToPeekReturns2DifferentItems()
        {
            // Setup - create a storage with 2 items
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            Transmission firstTransmission = CreateTransmissionAndEnqueueIt(storage);            
            Transmission secondTransmission = CreateTransmissionAndEnqueueIt(storage);            

            // Act
            StorageTransmission firstPeekedTransmission = storage.Peek();
            StorageTransmission secondPeekedTransmission = storage.Peek();

            // Asserts            
            Assert.IsNotNull(firstPeekedTransmission);
            Assert.IsNotNull(secondPeekedTransmission);

            string first = Encoding.UTF8.GetString(firstPeekedTransmission.Content, 0, firstPeekedTransmission.Content.Length);
            string second = Encoding.UTF8.GetString(secondPeekedTransmission.Content, 0, secondPeekedTransmission.Content.Length);
            Assert.AreNotEqual(first, second);
        }

        [TestMethod]
        public void WhenMaxFilesIsOneThenSecondTranmissionIsDropped()
        {
            // Setup
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            storage.MaxFiles = 1;

            // Act - Enqueue twice
            CreateTransmissionAndEnqueueIt(storage);
            CreateTransmissionAndEnqueueIt(storage);

            // Asserts - Second Peek should be null 
            Assert.IsNotNull(storage.Peek());
            Assert.IsNull(storage.Peek());
        }

        [TestMethod]
        public void WhenMaxSizeIsReachedThenEnqueuedTranmissionsAreDropped()
        {
            // Setup - create a storage with 2 items
            Storage storage = new Storage("unittest" + Guid.NewGuid().ToString());
            storage.CapacityInBytes = 200; // Each file enqueued in CreateTransmissionAndEnqueueIt is ~300 bytes.

            // Act - Enqueue twice
            CreateTransmissionAndEnqueueIt(storage);
            CreateTransmissionAndEnqueueIt(storage);

            // Asserts - Second Peek should be null 
            Assert.IsNotNull(storage.Peek());
            Assert.IsNull(storage.Peek());
        }

        private static Transmission CreateTransmission(ITelemetry telemetry)
        {
            byte[] data = JsonSerializer.Serialize(telemetry);
            Transmission transmission = new Transmission(
                                new Uri(@"http://some.url"),
                                data,
                                "application/x-json-stream",
                                JsonSerializer.CompressionType);

            return transmission;
        }

        private static Transmission CreateTransmissionAndEnqueueIt(Storage storage)
        {
            Transmission firstTransmission = CreateTransmission(new TraceTelemetry(Guid.NewGuid().ToString()));
            storage.EnqueueAsync(firstTransmission).ConfigureAwait(false).GetAwaiter().GetResult();

            return firstTransmission;
        }
    }
}
