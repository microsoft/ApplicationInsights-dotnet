// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpHelper40.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Shared HTTP helper class to make outbound http calls for DOT NET FW 4.0
// </summary>

namespace HttpSQLHelpers
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;

    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.Azure;

    /// <summary>
    /// Contains static methods to help make outbound http calls
    /// </summary>
    [ComVisible(false)]
    public class HttpHelpers
    {
        /// <summary>
        /// Invalid endpoint to trigger exception being thrown
        /// </summary>
        public const string UrlWhichThrowException = "http://google.com/404";

        /// <summary>
        /// Invalid endpoint to trigger exception being thrown at DNS resolution
        /// </summary>
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";

        public static CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(
                    string.Format(CloudConfigurationManager.GetSetting("StorageConnectionString"),
                CloudConfigurationManager.GetSetting("azureemulatorhostname")));

        /// <summary>
        /// Make sync http calls
        /// </summary>                
        public static void MakeHttpCallSync(string targetUrl)
        {
            try
            {
                Uri ourUri = new Uri(targetUrl);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                using (var stm = myHttpWebResponse.GetResponseStream())
                {
                    using (var reader = new StreamReader(stm))
                    {
                        var content = reader.ReadToEnd();
                    }
                }
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Async, Await keywords are used to achieve async calls. 
        /// </summary>                  
        public static async void MakeHttpCallAsyncAwait1(string targetUrl)
        {
            try
            {
                Uri ourUri = new Uri(targetUrl);
                WebRequest wr = WebRequest.Create(ourUri);
                var response = await wr.GetResponseAsync();
                using (var stm = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stm))
                    {
                        var content = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
        }

        /// <summary>
        /// Make sync http calls using http client
        /// </summary>
        public static string MakeHttpCallUsingHttpClient(string targetUrl)
        {
            try
            {
                HttpClient httpClient = new HttpClient();
                return httpClient.GetStringAsync(new Uri(targetUrl)).Result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
                return "";
            }
        }

        /// <summary>
        /// Make sync http calls with POST
        /// </summary>                
        public static void MakeHttpPostCallSync(string targetUrl)
        {            
            HttpClient client = new HttpClient();
            var content = ("helloworld");
            client.PostAsync(targetUrl, new StringContent(content.ToString(), Encoding.UTF8, "application/json")).Wait();
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread execution is blocked by calling End method
        /// </summary>        
        public static void MakeHttpCallAsync1(string targetUrl)
        {
            try
            {
                Uri ourUri = new Uri(targetUrl);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(null, null);
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread execution is blocked by calling Wait on the async wait handle
        /// </summary>                
        public static void MakeHttpCallAsync2(string targetUrl)
        {
            try
            {
                Uri ourUri = new Uri(targetUrl);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(null, null);
                result.AsyncWaitHandle.WaitOne();
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
        }       

        /// <summary>
        /// Make async http calls.
        /// Here, main thread does independent job while async call happens.
        /// Main thread checks if the async operation is completed by checking IsCompleted status
        /// </summary>                
        public static void MakeHttpCallAsync3(string targetUrl)
        {
            try
            {
                Uri ourUri = new Uri(targetUrl);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;              
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(null, null);

                // Poll for completion information.             
                while (result.IsCompleted != true)
                {
                    // Do something here
                }

                // Async operation is now complete. Read the result.
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();                
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured (as expected):" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread does independent job while async call happens.
        /// Main thread sets up a call back delegate which is automatically invoked when operation is completed
        /// </summary>                
        public static void MakeHttpCallAsync4(string targetUrl)
        {
            HttpWebRequest myHttpWebRequest = null;
            IAsyncResult result = null;
            try
            {
                // Create the delegate that will process the results of the  
                // asynchronous request.
                AsyncCallback callBack = new AsyncCallback(CallBackForHttp);

                Uri ourUri = new Uri(targetUrl);
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(callBack, myHttpWebRequest);
            }
            catch (Exception ex)
            {
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
                Trace.WriteLine("Exception occured:" + ex);
            }
        }

        /// <summary>
        /// Make azure call to read Blob
        /// </summary>                
        public static void MakeAzureCallToReadBlobWithSdk(string containerName, string blobName)
        {            
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // Retrieve reference to a blob named "testblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

            Directory.CreateDirectory(@"c:\fromblob");

            // Save blob contents to a file.
            using (var fileStream = File.OpenWrite(@"c:\fromblob\testblob"))
            {
                blockBlob.DownloadToStream(fileStream);
            }

            Directory.Delete(@"c:\fromblob", true);

            blockBlob.DeleteIfExists();
            container.DeleteIfExists();

        }

        /// <summary>
        /// Make azure call to write to Blob
        /// </summary>                
        public static void MakeAzureCallToWriteToBlobWithSdk(string containerName, string blobName)
        {
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            container.CreateIfNotExists();

            // Retrieve reference to a blob named "testblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);

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

        /// <summary>
        /// Make azure call to write to Table
        /// </summary>                
        public static void MakeAzureCallToWriteTableWithSdk(string tableName)
        {            
            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

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
        /// Make azure call to read from Table
        /// </summary>                
        public static void MakeAzureCallToReadTableWithSdk(string tableName)
        {            
            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();

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

            table.DeleteIfExists();
        }

        /// <summary>
        /// Make azure call to write to Queue
        /// </summary>                
        public static void MakeAzureCallToWriteQueueWithSdk()
        {        
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

        /// <summary>
        /// The following method is called when each asynchronous operation completes.         
        /// </summary>        
        /// <param name="result">The async result obtain from begin operation</param>
        private static void CallBackForHttp(IAsyncResult result)
        {
            try
            {
                // Async operation is now complete. Read the result.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)result.AsyncState;
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured:" + ex);
            }
        }
    }



    [ComVisible(false)]
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
