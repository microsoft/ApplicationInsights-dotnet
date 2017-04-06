// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HttpHelper40.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Shared HTTP helper class to make outbound http calls for DOT NET FW 4.0
// </summary>

namespace FW40Shared
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

    /// <summary>
    /// Contains static methods to help make outbound http calls
    /// </summary>
    [ComVisible(false)]
    public class HttpHelper40
    {
        /// <summary>
        /// Invalid endpoint to trigger exception being thrown
        /// </summary>
        public const string UrlWhichThrowException = "http://google.com/404";

        /// <summary>
        /// Invalid endpoint to trigger exception being thrown at DNS resolution
        /// </summary>
        private const string UrlWithNonexistentHostName = "http://abcdefzzzzeeeeadadad.com";

        /// <summary>
        /// Make sync http calls
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Reviewed manually")]
        public static void MakeHttpCallSync(int count, string hostname)
        {
            Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://www.{0}.com", hostname));
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            for (int i = 0; i < count; i++)
            {
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
        }

        public static string MakeHttpCallUsingHttpClient(string url)
        {
            HttpClient httpClient = new HttpClient();
            return httpClient.GetStringAsync(new Uri(url)).Result;
        }

        /// <summary>
        /// Make sync http calls with POST
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Reviewed manually")]
        public static void MakeHttpPostCallSync(int count, string hostname)
        {
            Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://www.{0}.com", hostname));
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            for (int i = 0; i < count; i++)
            {
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);

                var postData = "thing1=hello";
                postData += "&thing2=world";
                var data = Encoding.ASCII.GetBytes(postData);
                myHttpWebRequest.Method = "POST";
                myHttpWebRequest.ContentLength = data.Length;
                var stream = myHttpWebRequest.GetRequestStream();
                stream.Write(data, 0, data.Length);

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
        }

        /// <summary>
        /// Make sync http calls which fails
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Reviewed manually")]
        public static void MakeHttpCallSyncFailed(int count, bool simulateFailureAtDns = false)
        {
            try
            {
                Uri ourUri = new Uri(simulateFailureAtDns? UrlWithNonexistentHostName : UrlWhichThrowException);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                for (int i = 0; i < count; i++)
                {
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
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured (as expected):" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread execution is blocked by calling End method
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>      
        public static void MakeHttpCallAsync1(int count, string hostname)
        {
            Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://www.{0}.com", hostname));
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            IAsyncResult result = null;
            for (int i = 0; i < count; i++)
            {
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(null, null);
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
            }
        }

        /// <summary>
        /// Make async http calls which fails.
        /// Here, main thread execution is blocked by calling End method
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        public static void MakeHttpCallAsync1Failed(int count)
        {
            try
            {
                Uri ourUri = new Uri(UrlWhichThrowException);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;
                for (int i = 0; i < count; i++)
                {
                    myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                    result = myHttpWebRequest.BeginGetResponse(null, null);
                    myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                    myHttpWebResponse.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured (as expected):" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread execution is blocked by calling Wait on the async wait handle
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>     
        public static void MakeHttpCallAsync2(int count, string hostname)
        {
            Uri ourUri = new Uri("http://www.bing.com");
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            IAsyncResult result = null;
            for (int i = 0; i < count; i++)
            {
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(null, null);
                result.AsyncWaitHandle.WaitOne();
                myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread execution is blocked by calling Wait on the async wait handle
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        public static void MakeHttpCallAsync2Failed(int count)
        {
            try
            {
                Uri ourUri = new Uri(UrlWhichThrowException);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;
                for (int i = 0; i < count; i++)
                {
                    myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                    result = myHttpWebRequest.BeginGetResponse(null, null);
                    result.AsyncWaitHandle.WaitOne();
                    myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                    myHttpWebResponse.Close();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exception occured (as expected):" + ex);
            }
        }

        /// <summary>
        /// Make async http calls.
        /// Here, main thread does independent job while async call happens.
        /// Main thread checks if the async operation is completed by checking IsCompleted status
        /// </summary>        
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>    
        public static void MakeHttpCallAsync3(int count, string hostname)
        {
            Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://www.{0}.com", hostname));
            HttpWebRequest myHttpWebRequest = null;
            HttpWebResponse myHttpWebResponse = null;
            IAsyncResult result = null;
            for (int i = 0; i < count; i++)
            {
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
        }

        /// <summary>
        /// Make async http calls which fails.
        /// Here, main thread does independent job while async call happens.
        /// Main thread checks if the async operation is completed by checking IsCompleted status
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        public static void MakeHttpCallAsync3Failed(int count)
        {
            try
            {
                Uri ourUri = new Uri(UrlWhichThrowException);
                HttpWebRequest myHttpWebRequest = null;
                HttpWebResponse myHttpWebResponse = null;
                IAsyncResult result = null;
                for (int i = 0; i < count; i++)
                {
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
        /// <param name="count">no of calls to be made</param>        
        /// <param name="hostname">the call will be made to http://www.hostname.com</param>    
        public static void MakeHttpCallAsync4(int count, string hostname)
        {
            // Create the delegate that will process the results of the  
            // asynchronous request.
            AsyncCallback callBack = new AsyncCallback(CallBackForHttp);

            Uri ourUri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://www.{0}.com", hostname));
            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
            IAsyncResult result = myHttpWebRequest.BeginGetResponse(callBack, myHttpWebRequest);          
        }

        /// <summary>
        /// Make async http calls which fails.
        /// Here, main thread does independent job while async call happens.
        /// Main thread sets up a call back delegate which is automatically invoked when operation is completed
        /// </summary>        
        /// <param name="count">no of calls to be made</param>                
        public static void MakeHttpCallAsync4Failed(int count)
        {
            HttpWebRequest myHttpWebRequest = null;
            IAsyncResult result = null;
            try
            {
                // Create the delegate that will process the results of the  
                // asynchronous request.
                AsyncCallback callBack = new AsyncCallback(CallBackForHttp);

                Uri ourUri = new Uri(UrlWhichThrowException);
                myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                result = myHttpWebRequest.BeginGetResponse(callBack, myHttpWebRequest);                
            }
            catch (Exception ex)
            {
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                myHttpWebResponse.Close();
                Trace.WriteLine("Exception occured (as expected):" + ex);
            }
        }

        /// <summary>
        /// Make Azure calls to table, blob and queue using SDK
        /// </summary>        
        /// <param name="count">no of calls to be made</param> 
        public static void MakeAzureSdkCalls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                MakeAzureCallToReadBlobWithSdk(1);
                MakeAzureCallToWriteTableWithSdk(1);
                MakeAzureCallToReadTableWithSdk(1);
                MakeAzureCallToWriteQueueWithSdk(1);
            }
        }

        /// <summary>
        /// Make Azure calls to table, blob and queue using SDK
        /// </summary>        
        /// <param name="count">no of calls to be made</param> 
        public static void MakeAzureBlobCalls(int count)
        {
            for (int i = 0; i < count; i++)
            {
                MakeAzureCallToReadBlobWithSdk(1);                
            }
        }

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

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference("people");
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
        /// <param name="count">no of calls to be made</param>        
        public static void MakeAzureCallToReadTableWithSdk(int count)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the table if it doesn't exist.
            CloudTable table = tableClient.GetTableReference("people");
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
                Trace.WriteLine("Exception occured (as expected):" + ex);
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
