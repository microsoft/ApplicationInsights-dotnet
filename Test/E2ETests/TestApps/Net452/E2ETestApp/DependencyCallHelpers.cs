using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace E2ETestApp
{
    public class DependencyCallHelpers
    {
        /// <summary>
        /// Make azure call to read Blob
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToReadBlobWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("rddtest");

            // Retrieve reference to a blob named "testblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("testblob");

            Directory.CreateDirectory(@"c:\fromblob");

            // Save blob contents to a file.
            using (var fileStream = File.OpenWrite(@"c:\fromblob\testblob"))
            {
                blockBlob.DownloadToStream(fileStream);
            }

            Directory.Delete(@"c:\fromblob", true);
        }

        /// <summary>
        /// Make azure call to write Blob
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToWriteBlobWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("rddtest");

            // Retrieve reference to a blob named "testblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("testblob");

            Directory.CreateDirectory(@"c:\fromblob");

            // Save blob contents to a file.
            using (var fileStream = File.OpenWrite(@"c:\fromblob\testblob"))
            {
                blockBlob.DownloadToStream(fileStream);
            }

            Directory.Delete(@"c:\fromblob", true);
        }

        /// <summary>
        /// Make azure call to write to Table
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToWriteTableWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // people table is assumed to be present. About.aspx creates it.
            CloudTable table = tableClient.GetTableReference("people");            

            // Create the batch operation.
            TableBatchOperation batchOperation = new TableBatchOperation();

            // Create a customer entity and add it to the table.
            CustomerEntity customer1 = new CustomerEntity("Smith", "Jeff" + DateTime.UtcNow.Ticks);
            customer1.Email = "Jeff@contoso.com";
            customer1.PhoneNumber = "425-555-0104";

            // Create another customer entity and add it to the table.
            CustomerEntity customer2 = new CustomerEntity("Smith", "Ben" + DateTime.UtcNow.Ticks);
            customer2.Email = "Ben@contoso.com";
            customer2.PhoneNumber = "425-555-0102";

            // Add both customer entities to the batch insert operation.
            batchOperation.Insert(customer1);
            batchOperation.Insert(customer2);

            // Execute the batch operation.
            table.ExecuteBatch(batchOperation);
        }

        /// <summary>
        /// Make azure call to write to Table
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToCreateTableWithSdk(string tableName)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();          
        }

        /// <summary>
        /// Make azure call to read from Table
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToReadTableWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // table should already be present.
            CloudTable table = tableClient.GetTableReference("people");            

            // Create the table query.
            TableQuery<CustomerEntity> rangeQuery = new TableQuery<CustomerEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "Smith"),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, "E")));

            // Loop through the results, displaying information about the entity.
            foreach (CustomerEntity entity in table.ExecuteQuery(rangeQuery))
            {
                var x1 = entity.PartitionKey;
                var x2 = entity.RowKey;
                var x3 = entity.Email;
                var x4 = entity.PhoneNumber;
                var s = x1 + x2 + x3 + x4;
            }
        }

        /// <summary>
        /// Make azure call to write to Queue
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToWriteQueueWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the queue client
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            // Retrieve a reference to a queue
            CloudQueue queue = queueClient.GetQueueReference("myqueue");
            queue.CreateIfNotExists();

            // Create a message and add it to the queue.
            CloudQueueMessage message = new CloudQueueMessage("Hello, World" + DateTime.UtcNow);
            queue.AddMessage(message);

            queue.DeleteIfExists();
        }
    }

    public class CustomerEntity : TableEntity
    {
        public CustomerEntity(string lastName, string firstName)
        {
            this.PartitionKey = lastName;
            this.RowKey = firstName;
        }

        public CustomerEntity() { }

        public string Email { get; set; }

        public string PhoneNumber { get; set; }
    }
}