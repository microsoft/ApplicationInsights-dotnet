// <copyright file="PersistenceChannel.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

#if WINRT
    using TaskEx = System.Threading.Tasks.Task;    
#endif

    /// <summary>
    /// Represents a communication channel for sending telemetry to Application Insights via HTTPS.
    /// </summary>
    public sealed class PersistenceChannel : ITelemetryChannel
    {
        internal readonly TelemetryBuffer TelemetryBuffer;
        internal PersistenceTransmitter Transmitter;

        private readonly FlushManager flushManager;
        private bool? developerMode;
        private int disposeCount;
        private int telemetryBufferSize;
        private Storage storage;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceChannel"/> class.
        /// </summary>
        public PersistenceChannel()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceChannel"/> class.
        /// </summary>
        /// <param name="storageFolderName">
        /// A folder name. Under this folder all the transmissions will be saved. 
        /// Setting this value groups channels, even from different processes. 
        /// If 2 (or more) channels has the same <c>storageFolderName</c> only one channel will perform the sending even if the channel is in a different process/AppDomain/Thread.  
        /// </param>
        /// <param name="sendersCount">
        /// Defines the number of senders. A sender is a long-running thread that sends telemetry batches in intervals defined by <see cref="SendingInterval"/>. 
        /// So the amount of senders also defined the maximum amount of http channels opened at the same time.
        /// </param>        
        public PersistenceChannel(string storageFolderName, int sendersCount = 3)
        {   
            this.TelemetryBuffer = new TelemetryBuffer();
            this.storage = new Storage(storageFolderName);
            this.Transmitter = new PersistenceTransmitter(this.storage, sendersCount);
            this.flushManager = new FlushManager(this.storage, this.TelemetryBuffer);
            this.EndpointAddress = Constants.TelemetryServiceEndpoint;
            this.developerMode = false;
        }

        /// <summary>
        /// Gets the storage unique folder.
        /// </summary>
        public string StorageUniqueFolder
        {
            get
            {
                return this.Transmitter.StorageUniqueFolder;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether developer mode of telemetry transmission is enabled.
        /// When developer mode is True, <see cref="PersistenceChannel"/> sends telemetry to Application Insights immediately 
        /// during the entire lifetime of the application. When developer mode is False, <see cref="PersistenceChannel"/>
        /// respects production sending policies defined by other properties.
        /// </summary>
        public bool? DeveloperMode 
        {
            get 
            { 
                return this.developerMode; 
            }

            set
            {
                if (value != this.developerMode)
                {
                    if (value.HasValue && value.Value)
                    {
                        this.telemetryBufferSize = this.TelemetryBuffer.Capacity;
                        this.TelemetryBuffer.Capacity = 1;
                    }
                    else
                    {
                        this.TelemetryBuffer.Capacity = this.telemetryBufferSize;
                    }

                    this.developerMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets an interval between each successful sending.
        /// </summary>
        /// <remarks>On error scenario this value is ignored and the interval will be defined using an exponential back-off algorithm.</remarks>
        public TimeSpan? SendingInterval 
        { 
            get
            {
                return this.Transmitter.SendingInterval;
            }

            set
            {
                this.Transmitter.SendingInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the interval between each flush to disk. 
        /// </summary>
        public TimeSpan FlushInterval
        {
            get
            {
                return this.flushManager.FlushDelay;
            }

            set
            {
                this.flushManager.FlushDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP address where the telemetry is sent.
        /// </summary>
        public string EndpointAddress
        {
            get 
            {
                return this.flushManager.EndpointAddress.ToString(); 
            }

            set 
            {
                string address = value ?? Constants.TelemetryServiceEndpoint;
                this.flushManager.EndpointAddress = new Uri(address);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items that will accumulate in a memory before 
        /// <see cref="PersistenceChannel"/> persists them to disk.
        /// </summary>
        public int MaxTelemetryBufferCapacity 
        {
            get { return this.TelemetryBuffer.Capacity; }
            set { this.TelemetryBuffer.Capacity = value; } 
        }

        /// <summary>
        /// Gets or sets the maximum amount of disk space, in bytes, that <see cref="PersistenceChannel"/> will 
        /// use for storage.
        /// </summary>
        public ulong MaxTransmissionStorageCapacity
        {
            get { return this.storage.CapacityInBytes; }
            set { this.storage.CapacityInBytes = value; }
        }

        /// <summary>
        /// Gets or sets the maximum amount of files allowed in storage. When the limit is reached telemetries will be dropped.
        /// </summary>
        public uint MaxTransmissionStorageFilesCapacity
        {
            get { return this.storage.MaxFiles; }
            set { this.storage.MaxFiles = value; }
        }

        /// <summary>
        /// Gets or sets the amount of time, in seconds, after application is started when the 
        /// <see cref="PersistenceChannel"/> will send telemetry to ApplicationInsights. Once the specified 
        /// amount of time runs out, telemetry will be stored on disk until the application is started again.
        /// </summary>
        [Obsolete("This value is now obsolete and will be removed in next release. Currently it does nothing.")]
        public double StopUploadAfterIntervalInSeconds // TODO: this should be a TimeSpan
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum telemetry batching interval. Once the interval expires, <see cref="PersistenceChannel"/> 
        /// persists the accumulated telemetry items.
        /// </summary>
        [Obsolete("This value is now obsolete and will be removed in next release, use FlushInterval instead.")]
        public double DataUploadIntervalInSeconds // TODO: this should be a TimeSpan
        {
            get { return this.flushManager.FlushDelay.Seconds; }
            set { this.flushManager.FlushDelay = TimeSpan.FromSeconds(value); }
        }

        /// <summary>
        /// Gets or sets the maximum amount of memory, in bytes, that <see cref="PersistenceChannel"/> will use 
        /// to buffer transmissions before sending them to Application Insights.
        /// </summary>
        [Obsolete("This value is now obsolete and will be removed in next release. Currently it does nothing.")]
        public int MaxTransmissionBufferCapacity
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum number of telemetry transmissions that <see cref="PersistenceChannel"/> will 
        /// send to Application Insights at the same time.
        /// </summary>
        [Obsolete("This value is now obsolete and will be removed in next release, use the sendersCount parameter in the constructor instead.")]
        public int MaxTransmissionSenderCapacity
        {
            get;
            set;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Increment(ref this.disposeCount) == 1)
            {
                if (this.Transmitter != null)
                {
                    this.Transmitter.Dispose();
                }

                if (this.flushManager != null)
                {
                    this.flushManager.Dispose();
                }
            }
        }

        /// <summary>
        /// Sends an instance of ITelemetry through the channel.
        /// </summary>
        public void Send(ITelemetry item)
        {
            if (this.DeveloperMode.HasValue && this.DeveloperMode == true)
            {
                this.Transmitter.SendForDeveloperMode(item, this.EndpointAddress);
            }
            else
            {
                this.TelemetryBuffer.Enqueue(item);
            }
        }

        /// <summary>
        /// Flushes the in-memory buffer to disk. 
        /// </summary>
        public void Flush()
        {   
            this.flushManager.Flush();
        }        
    }
}
