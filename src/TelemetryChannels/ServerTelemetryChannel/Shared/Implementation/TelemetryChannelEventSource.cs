namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
#if NET40  
    using System.Net;
    using Microsoft.Diagnostics.Tracing;
#else
    using System.Diagnostics.Tracing;
    using System.Net;

#endif

    [EventSource(Name = "Microsoft-ApplicationInsights-WindowsServer-TelemetryChannel")]
    internal sealed class TelemetryChannelEventSource : EventSource
    {
        public static readonly TelemetryChannelEventSource Log = new TelemetryChannelEventSource();

        private TelemetryChannelEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { get; private set; }

        public bool IsVerboseEnabled
        {
            get
            {
                return Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        [Event(11, Keywords = Keywords.TelemetryChannel, Message = "Storage folder: {0}.", Level = EventLevel.Verbose)]
        public void StorageFolder(string folder, string appDomainName = "Incorrect")
        {
            this.WriteEvent(11, folder ?? string.Empty, this.ApplicationName);
        }

        [Event(12, Keywords = Keywords.TelemetryChannel, Message = "BufferEnqueued. TransmissionId: {0}. TransmissionCount: {1}.", Level = EventLevel.Verbose)]
        public void BufferEnqueued(string transmissionId, int transmissionCount, string appDomainName = "Incorrect")
        {
            this.WriteEvent(12, transmissionId ?? string.Empty, transmissionCount, this.ApplicationName);
        }

        [Event(13, Keywords = Keywords.TelemetryChannel, Message = "BufferEnqueueNoCapacity. Size: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void BufferEnqueueNoCapacityWarning(long size, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(13, size, capacity, this.ApplicationName);
        }

        [Event(14, Keywords = Keywords.TelemetryChannel, Message = "UnauthorizedAccessExceptionOnTransmissionSave. TransmissionId: {0}. Message: {1}.", Level = EventLevel.Warning)]
        public void UnauthorizedAccessExceptionOnTransmissionSaveWarning(string transmissionId, string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(14, transmissionId ?? string.Empty, message ?? string.Empty, this.ApplicationName);
        }

        [Event(15, Keywords = Keywords.TelemetryChannel, Message = "StorageSize. StorageSize: {0}.", Level = EventLevel.Verbose)]
        public void StorageSize(long size, string appDomainName = "Incorrect")
        {
            this.WriteEvent(15, size, this.ApplicationName);
        }

        [Event(16, Keywords = Keywords.TelemetryChannel, Message = "SenderEnqueueNoCapacity. TransmissionCount: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void SenderEnqueueNoCapacityWarning(int transmissionCount, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(16, transmissionCount, capacity, this.ApplicationName);
        }

        [Event(17, Keywords = Keywords.TelemetryChannel, Message = "TransmissionSendStarted. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmissionSendStarted(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(17, id ?? string.Empty, this.ApplicationName);
        }

        [Event(18, Keywords = Keywords.TelemetryChannel, Message = "SerializationStarted. Items count: {0}", Level = EventLevel.Verbose)]
        public void SerializationStarted(int count, string appDomainName = "Incorrect")
        {
            this.WriteEvent(18, count, this.ApplicationName);
        }

        [Event(19, Keywords = Keywords.TelemetryChannel, Message = "Transmitter flushed telemetry events.", Level = EventLevel.Verbose)]
        public void TelemetryChannelFlush(string appDomainName = "Incorrect")
        {
            this.WriteEvent(19, this.ApplicationName);
        }
        
        [Event(20, Keywords = Keywords.TelemetryChannel, Message = "{0} passed to channel", Level = EventLevel.Verbose)]
        public void TelemetryChannelSend(string type, string appDomainName = "Incorrect")
        {
            this.WriteEvent(20, type, this.ApplicationName);
        }

        [Event(21, Keywords = Keywords.TelemetryChannel, Message = "TransmitterEnqueue. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterEnqueue(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(21, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(22, Keywords = Keywords.TelemetryChannel, Message = "TransmissionSent. TransmissionId: {0}. Capacity: {1}.", Level = EventLevel.Verbose)]
        public void TransmissionSentSuccessfully(string transmissionId, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(22, transmissionId ?? string.Empty, capacity, this.ApplicationName);
        }

        [Event(23, Keywords = Keywords.TelemetryChannel, Message = "TransmissionSendingFailed. TransmissionId: {0}. Message: {1}. StatusCode: {2}.", Level = EventLevel.Warning)]
        public void TransmissionSendingFailedWebExceptionWarning(string transmissionId, string exceptionMessage, HttpStatusCode statusCode, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                23,
                transmissionId ?? string.Empty,
                exceptionMessage ?? string.Empty,
                statusCode,
                this.ApplicationName);
        }

        [Event(24, Keywords = Keywords.TelemetryChannel, Message = "Transmission policy failed with parsing Retry-After http header: '{0}'", Level = EventLevel.Warning)]
        public void TransmissionPolicyRetryAfterParseFailedWarning(string retryAfterHeader, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                24,
                retryAfterHeader ?? string.Empty,
                this.ApplicationName);
        }

        [Event(25, Keywords = Keywords.TelemetryChannel, Message = "StorageEnqueueNoCapacity. Size: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void StorageEnqueueNoCapacityWarning(long size, long capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(25, size, capacity, this.ApplicationName);
        }

        [Event(26, Keywords = Keywords.TelemetryChannel, Message = "TransmissionSavedToStorage. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmissionSavedToStorage(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(26, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(27, Keywords = Keywords.TelemetryChannel, Message = "{0} changed sender capacity to {1}", Level = EventLevel.Verbose)]
        public void SenderCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(27, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(28, Keywords = Keywords.TelemetryChannel, Message = "{0} changed buffer capacity to {1}", Level = EventLevel.Verbose)]
        public void BufferCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(28, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(29, Keywords = Keywords.TelemetryChannel, Message = "SenderCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void SenderCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(29, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(30, Keywords = Keywords.TelemetryChannel, Message = "BufferCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void BufferCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(30, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(31, Keywords = Keywords.TelemetryChannel, Message = "BackoffTimeSetInSeconds: {0}", Level = EventLevel.Verbose)]
        public void BackoffTimeSetInSeconds(double seconds, string appDomainName = "Incorrect")
        {
            this.WriteEvent(31, seconds, this.ApplicationName);
        }

        [Event(32, Keywords = Keywords.TelemetryChannel, Message = "NetworkIsNotAvailable: {0}", Level = EventLevel.Warning)]
        public void NetworkIsNotAvailableWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(32, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(33, Keywords = Keywords.TelemetryChannel, Message = "StorageCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void StorageCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(33, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(34, Keywords = Keywords.TelemetryChannel, Message = "{0} changed storage capacity to {1}", Level = EventLevel.Verbose)]
        public void StorageCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(34, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(35, Keywords = Keywords.TelemetryChannel, Message = "ThrottlingRetryAfterParsedInSec: {0}", Level = EventLevel.Verbose)]
        public void ThrottlingRetryAfterParsedInSec(double retryAfter, string appDomainName = "Incorrect")
        {
            this.WriteEvent(35, retryAfter, this.ApplicationName);
        }

        [Event(36, Keywords = Keywords.TelemetryChannel, Message = "TransmitterEmptyStorage", Level = EventLevel.Verbose)]
        public void TransmitterEmptyStorage(string appDomainName = "Incorrect")
        {
            this.WriteEvent(36, this.ApplicationName);
        }

        [Event(37, Keywords = Keywords.TelemetryChannel, Message = "TransmitterEmptyBuffer", Level = EventLevel.Verbose)]
        public void TransmitterEmptyBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(37, this.ApplicationName);
        }

        [Event(38, Keywords = Keywords.TelemetryChannel, Message = "SubscribeToNetworkFailure: {0}", Level = EventLevel.Warning)]
        public void SubscribeToNetworkFailureWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(38, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(39, Keywords = Keywords.TelemetryChannel, Message = "SubscribeToNetworkFailure: {0}", Level = EventLevel.Warning)]
        public void ExceptionHandlerStartExceptionWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(39, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(40, Keywords = Keywords.TelemetryChannel, Message = "TransmitterSenderSkipped. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterSenderSkipped(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(40, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(41, Keywords = Keywords.TelemetryChannel, Message = "TransmitterBufferSkipped. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterBufferSkipped(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(41, transmissionId, this.ApplicationName);
        }

        [Event(42, Keywords = Keywords.TelemetryChannel, Message = "TransmitterStorageSkipped. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterStorageSkipped(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(42, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(43, Keywords = Keywords.TelemetryChannel, Message = "IncorrectFileFormat. Error: {0}.", Level = EventLevel.Warning)]
        public void IncorrectFileFormatWarning(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(43, errorMessage ?? string.Empty, this.ApplicationName);
        }

        [Event(44, Keywords = Keywords.TelemetryChannel, Message = "Unexpected exception when handling IApplicationLifecycle.Stopping event:{0}", Level = EventLevel.Error)]
        public void UnexpectedExceptionInStopError(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(44, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(45, Keywords = Keywords.TelemetryChannel, Message = "Transmission polices failed to execute. Exception:{0}", Level = EventLevel.Error)]
        public void ApplyPoliciesError(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(45, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(46, Keywords = Keywords.TelemetryChannel, Message = "Transmission polices failed to execute. Exception:{0}", Level = EventLevel.Warning)]
        public void TelemetryChannelInitailizeFailedWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(46, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(48, Keywords = Keywords.TelemetryChannel, Message = "TransmissionFailedToStoreWarning. TransmissionId: {0}. Exception: {1}.", Level = EventLevel.Warning)]
        public void TransmissionFailedToStoreWarning(string transmissionId, string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(48, transmissionId ?? string.Empty, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(49, Keywords = Keywords.TelemetryChannel, Message = "MovedFromSenderToBuffer.", Level = EventLevel.Verbose)]
        public void MovedFromSenderToBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(49, this.ApplicationName);
        }

        [Event(50, Keywords = Keywords.TelemetryChannel, Message = "MovedFromStorageToSender.", Level = EventLevel.Verbose)]
        public void MovedFromStorageToSender(string appDomainName = "Incorrect")
        {
            this.WriteEvent(50, this.ApplicationName);
        }

        [Event(51, Keywords = Keywords.TelemetryChannel, Message = "MovedFromStorageToBuffer.", Level = EventLevel.Verbose)]
        public void MovedFromStorageToBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(51, this.ApplicationName);
        }

        [Event(52, Keywords = Keywords.TelemetryChannel, Message = "MovedFromBufferToStorage.", Level = EventLevel.Verbose)]
        public void MovedFromBufferToStorage(string appDomainName = "Incorrect")
        {
            this.WriteEvent(52, this.ApplicationName);
        }

        [Event(53, Keywords = Keywords.TelemetryChannel, Message = "UnauthorizedAccessExceptionOnCalculateSizeWarning. Message: {0}.", Level = EventLevel.Warning)]
        public void UnauthorizedAccessExceptionOnCalculateSizeWarning(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(53, message ?? string.Empty, this.ApplicationName);
        }

        [Event(54, Keywords = Keywords.TelemetryChannel, Message = "TransmissionSendingFailed. TransmissionId: {0}. Message: {1}.", Level = EventLevel.Warning)]
        public void TransmissionSendingFailedWarning(string transmissionId, string exceptionMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                54,
                transmissionId ?? string.Empty,
                exceptionMessage ?? string.Empty,
                this.ApplicationName);
        }

        [Event(
            55, 
            Keywords = Keywords.UserActionable | Keywords.TelemetryChannel,
            Message = "Access to the local storage was denied. If you want Application Insights SDK to store telemetry locally on disk in case of transient network issues please the process give access either to %LOCALAPPDATA% or to %TEMP% folder. After you give access to the folder you need to restart the process. Currently monitoring will continue but if telemetry cannot be sent it will be dropped.", 
            Level = EventLevel.Error)]
        public void TransmissionStorageAccessDeniedError(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                55,
                this.ApplicationName);
        }

        [Event(90, Keywords = Keywords.TelemetryChannel, Message = "[msg=Log Error];[msg={0}]", Level = EventLevel.Error)]
        public void LogError(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(90, msg ?? string.Empty, this.ApplicationName);
        }

        private string GetApplicationName()
        {
            string name;
            try
            {
                name = AppDomain.CurrentDomain.FriendlyName;
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp.Message;
            }

            return name;
        }

        public sealed class Keywords
        {
            public const EventKeywords UserActionable = (EventKeywords)0x1;

            public const EventKeywords Diagnostics = (EventKeywords)0x2;

            public const EventKeywords VerboseFailure = (EventKeywords)0x4;

            /*  Reserve first 3 for other service keywords
             *  public const EventKeywords Service2 = (EventKeywords)0x4;
             *  public const EventKeywords Service3 = (EventKeywords)0x8;
             */

            public const EventKeywords TelemetryChannel = (EventKeywords)0x10;
        }
    }
}
