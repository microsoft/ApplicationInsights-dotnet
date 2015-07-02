// <copyright file="PersistenceTransmitter.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    /// <summary>
    /// Implements throttled and persisted transmission of telemetry to Application Insights. 
    /// </summary>
    internal class PersistenceTransmitter : IDisposable
    {
        /// <summary>
        /// A list of senders that sends transmissions. 
        /// </summary>
        private List<Sender> senders = new List<Sender>();
        
        /// <summary>
        /// The storage that is used to persist all the transmissions. 
        /// </summary>
        private StorageBase storage;

        /// <summary>
        /// Cancels the sending. 
        /// </summary>
        private CancellationTokenSource sendingCancellationTokenSource;
        
        /// <summary>
        /// A mutex that will be used as a name mutex to synchronize transmitters even from different processes.
        /// </summary>
        private Mutex mutex;

        /// <summary>
        /// The number of times this object was disposed.
        /// </summary>
        private int disposeCount = 0;

        /// <summary>
        /// Mutex is released once the thread that acquired it is ended. This event keeps the long running thread that acquire the mutex alive until dispose is called.    
        /// </summary>
        private AutoResetEvent eventToKeepMutexThreadAlive;

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceTransmitter"/> class.
        /// </summary>
        /// <param name="storage">The transmissions storage.</param>
        /// <param name="sendersCount">The number of senders to create.</param>
        /// <param name="createSenders">A boolean value that indicates if this class should try and create senders. This is a workaround for unit tests purposes only.</param>
        internal PersistenceTransmitter(StorageBase storage, int sendersCount, bool createSenders = true)
        {
            this.storage = storage;
            this.sendingCancellationTokenSource = new CancellationTokenSource();
            this.mutex = new Mutex(initiallyOwned: false, name: this.storage.FolderName);
            this.eventToKeepMutexThreadAlive = new AutoResetEvent(false);

            if (createSenders)
            {
                Task.Factory.StartNew(() => this.AcquireMutex(() => this.CreateSenders(sendersCount)), TaskCreationOptions.LongRunning)
                .ContinueWith(
                    task =>
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture, "PersistenceTransmitter: Unhandled exception in CreateSenders: {0}", task.Exception);
                        CoreEventSource.Log.LogVerbose(msg);
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// Gets a unique folder name. This folder will be used to store the transmission files.
        /// </summary>
        internal string StorageUniqueFolder
        { 
            get
            {
                return this.storage.FolderName;
            }
        }

        /// <summary>
        /// Gets or sets the interval between each successful sending. 
        /// </summary>
        internal TimeSpan? SendingInterval
        {
            get;
            set;
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {   
           if (Interlocked.Increment(ref this.disposeCount) == 1)
           {
               this.sendingCancellationTokenSource.Cancel();
               this.sendingCancellationTokenSource.Dispose();               

#if NET35
               this.mutex.Close();
               this.eventToKeepMutexThreadAlive.Close();
#else
               this.mutex.Dispose();
               this.eventToKeepMutexThreadAlive.Dispose();
#endif

               this.StopSenders();
           }
        }

        /// <summary>
        /// Sending the item to the endpoint immediately without persistence.
        /// </summary>
        /// <param name="item">Telemetry item.</param>
        /// <param name="endpointAddress">Server endpoint address.</param>
        internal void SendForDeveloperMode(ITelemetry item, string endpointAddress)
        {
            try
            {
                byte[] data = JsonSerializer.Serialize(item);
                var transmission = new Transmission(new Uri(endpointAddress), data, "application/x-json-stream", JsonSerializer.CompressionType);

                transmission.SendAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                CoreEventSource.Log.LogVerbose("Failed sending event in developer mode Exception:" + exception);
            }
        }

        /// <summary>
        /// Make sure that <paramref name="action"/> happens only once even if it is executed on different processes. 
        /// On every given time only one channel will acquire the mutex, even if the channel is on a different process.        
        /// This method is using a named mutex to achieve that. Once the mutex is acquired <paramref name="action"/> will be executed.
        /// </summary>
        /// <param name="action">The action to perform once the mutex is acquired.</param>
        private void AcquireMutex(Action action)
        {
            while (!this.sendingCancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    this.mutex.WaitOne();

                    if (this.sendingCancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    if (action != null)
                    {
                        action();
                    }

                    // Prevent the thread for quiting. Once this thread ends the Mutex will be released and other transmitter 
                    // (from different processes for example, will acquire the mutex)
                    this.eventToKeepMutexThreadAlive.WaitOne();
                    return;
                }
                catch (AbandonedMutexException)
                {
                    CoreEventSource.Log.LogVerbose("Another process/thread abandon the Mutex, try to acquire it and become the active transmitter.");
                }
                catch (ObjectDisposedException)
                {
                    // if mutex or the auto reset event are disposed, just quit.
                    return; 
                }
            }
        }

        /// <summary>
        /// Create senders to send telemetries. 
        /// </summary>
        private void CreateSenders(int sendersCount)
        {
            for (int i = 0; i < sendersCount; i++)
            {
                this.senders.Add(new Sender(this.storage, this));
            }
        }

        /// <summary>
        /// Stops the senders.  
        /// </summary>
        /// <remarks>As long as there is no Start implementation, this method should only be called from Dispose.</remarks>
        private void StopSenders()
        {
            if (this.senders == null)
            {
                return;
            }

            var stoppedTasks = new List<Task>();
            foreach (var sender in this.senders)
            {
                stoppedTasks.Add(sender.StopAsync());
            }

            Task.WaitAll(stoppedTasks.ToArray());
        }
    }
}
