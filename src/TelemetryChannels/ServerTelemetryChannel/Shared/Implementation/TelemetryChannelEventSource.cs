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

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        public bool IsVerboseEnabled
        {
            [NonEvent]
            get
            {
                return Log.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        // Verbosity is Error - so it is always sent to portal; Keyword is Diagnostics so throttling is not applied.
        [Event(1,
            Message = "Diagnostic message: backoff logic disabled, transmission will be resolved.",
            Level = EventLevel.Error,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable)]
        public void BackoffDisabled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, this.ApplicationName);
        }

        // Verbosity is Error - so it is always sent to portal; Keyword is Diagnostics so throttling is not applied.
        [Event(2, 
            Message = "Diagnostic message: backoff logic was enabled. Backoff internal exceeded {0} min. Last status code received from the backend was {1}.", 
            Level = EventLevel.Error,
            Keywords = Keywords.Diagnostics | Keywords.UserActionable)]
        public void BackoffEnabled(double intervalInMin, int statusCode, string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, intervalInMin, statusCode, this.ApplicationName);
        }

        [Event(3, Message = "Sampling skipped: {0}.", Level = EventLevel.Verbose)]
        public void SamplingSkippedByType(string telemetryType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, telemetryType ?? string.Empty, this.ApplicationName);
        }

        [Event(4, Message = "Backoff interval in seconds {0}.", Level = EventLevel.Verbose)]
        public void BackoffInterval(double intervalInSec, string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, intervalInSec, this.ApplicationName);
        }

        [Event(5, Message = "Backend response {1} was not parsed. Some items may be dropped: {0}.", Level = EventLevel.Warning)]
        public void BreezeResponseWasNotParsedWarning(string exception, string response, string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, exception ?? string.Empty, response ?? string.Empty, this.ApplicationName);
        }

        [Event(6, Message = "Unexpected backend response. Items # in batch {0} >= Error index in response: {1}.", Level = EventLevel.Warning)]
        public void UnexpectedBreezeResponseWarning(int size, int index, string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, size, index, this.ApplicationName);
        }

        [Event(7, Message = "Item was rejected by endpoint. Message: {0}", Level = EventLevel.Warning)]
        public void ItemRejectedByEndpointWarning(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(7, message ?? string.Empty, this.ApplicationName);
        }

        [Event(8, Keywords = Keywords.UserActionable, Message = "User-defined sampling callback failed. Exception: {0}.", Level = EventLevel.Error)]
        public void SamplingCallbackError(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(8, exception, this.ApplicationName);
        }

        [Event(9, Message = "Sampling changed to {0}.", Level = EventLevel.Verbose)]
        public void SamplingChanged(double interval, string appDomainName = "Incorrect")
        {
            this.WriteEvent(9, interval, this.ApplicationName);
        }

        [Event(10, Message = "Sampled out: {0}.", Level = EventLevel.Verbose)]
        public void ItemSampledOut(string telemetryType, string appDomainName = "Incorrect")
        {
            this.WriteEvent(10, telemetryType ?? string.Empty, this.ApplicationName);
        }

        [Event(11, Message = "Storage folder: {0}.", Level = EventLevel.Verbose)]
        public void StorageFolder(string folder, string appDomainName = "Incorrect")
        {
            this.WriteEvent(11, folder ?? string.Empty, this.ApplicationName);
        }

        [Event(12, Message = "BufferEnqueued. TransmissionId: {0}. TransmissionCount: {1}.", Level = EventLevel.Verbose)]
        public void BufferEnqueued(string transmissionId, int transmissionCount, string appDomainName = "Incorrect")
        {
            this.WriteEvent(12, transmissionId ?? string.Empty, transmissionCount, this.ApplicationName);
        }

        [Event(13, Message = "BufferEnqueueNoCapacity. Size: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void BufferEnqueueNoCapacityWarning(long size, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(13, size, capacity, this.ApplicationName);
        }

        [Event(14, Message = "UnauthorizedAccessExceptionOnTransmissionSave. TransmissionId: {0}. Message: {1}.", Level = EventLevel.Warning)]
        public void UnauthorizedAccessExceptionOnTransmissionSaveWarning(string transmissionId, string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(14, transmissionId ?? string.Empty, message ?? string.Empty, this.ApplicationName);
        }

        [Event(15, Message = "StorageSize. StorageSize: {0}.", Level = EventLevel.Verbose)]
        public void StorageSize(long size, string appDomainName = "Incorrect")
        {
            this.WriteEvent(15, size, this.ApplicationName);
        }

        [Event(16, Message = "SenderEnqueueNoCapacity. TransmissionCount: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void SenderEnqueueNoCapacityWarning(int transmissionCount, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(16, transmissionCount, capacity, this.ApplicationName);
        }

        [Event(17, Message = "TransmissionSendStarted. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmissionSendStarted(string id, string appDomainName = "Incorrect")
        {
            this.WriteEvent(17, id ?? string.Empty, this.ApplicationName);
        }

        [Event(18, Message = "SerializationStarted. Items count: {0}", Level = EventLevel.Verbose)]
        public void SerializationStarted(int count, string appDomainName = "Incorrect")
        {
            this.WriteEvent(18, count, this.ApplicationName);
        }

        [Event(19, Message = "Transmitter flushed telemetry events.", Level = EventLevel.Verbose)]
        public void TelemetryChannelFlush(string appDomainName = "Incorrect")
        {
            this.WriteEvent(19, this.ApplicationName);
        }
        
        [Event(20, Message = "{0} passed to channel with iKey {1}...", Level = EventLevel.Verbose)]
        public void TelemetryChannelSend(string type, string key, string appDomainName = "Incorrect")
        {
            this.WriteEvent(20, type, key, this.ApplicationName);
        }

        [Event(21, Message = "TransmitterEnqueue. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterEnqueue(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(21, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(22, Message = "TransmissionSent. TransmissionId: {0}. Capacity: {1}.", Level = EventLevel.Verbose)]
        public void TransmissionSentSuccessfully(string transmissionId, int capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(22, transmissionId ?? string.Empty, capacity, this.ApplicationName);
        }

        [Event(23, Message = "TransmissionSendingFailed. TransmissionId: {0}. Message: {1}. StatusCode: {2}. Description: {3}.", Level = EventLevel.Warning)]
        public void TransmissionSendingFailedWebExceptionWarning(string transmissionId, string exceptionMessage, int statusCode, string description, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                23,
                transmissionId ?? string.Empty,
                exceptionMessage ?? string.Empty,
                statusCode,
                description ?? string.Empty,
                this.ApplicationName);
        }

        [Event(24, Message = "Transmission policy failed with parsing Retry-After http header: '{0}'", Level = EventLevel.Warning)]
        public void TransmissionPolicyRetryAfterParseFailedWarning(string retryAfterHeader, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                24,
                retryAfterHeader ?? string.Empty,
                this.ApplicationName);
        }

        [Event(25, Message = "StorageEnqueueNoCapacity. Size: {0}. Capacity: {1}.", Level = EventLevel.Warning)]
        public void StorageEnqueueNoCapacityWarning(long size, long capacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(25, size, capacity, this.ApplicationName);
        }

        [Event(26, Message = "TransmissionSavedToStorage. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmissionSavedToStorage(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(26, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(27, Message = "{0} changed sender capacity to {1}", Level = EventLevel.Verbose)]
        public void SenderCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(27, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(28, Message = "{0} changed buffer capacity to {1}", Level = EventLevel.Verbose)]
        public void BufferCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(28, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(29, Message = "SenderCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void SenderCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(29, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(30, Message = "BufferCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void BufferCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(30, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(31, Message = "BackoffTimeSetInSeconds: {0}", Level = EventLevel.Verbose)]
        public void BackoffTimeSetInSeconds(double seconds, string appDomainName = "Incorrect")
        {
            this.WriteEvent(31, seconds, this.ApplicationName);
        }

        [Event(32, Message = "NetworkIsNotAvailable", Level = EventLevel.Warning)]
        public void NetworkIsNotAvailableWarning(string appDomainName = "Incorrect")
        {
            this.WriteEvent(32, this.ApplicationName);
        }

        [Event(33, Message = "StorageCapacityReset: {0}", Level = EventLevel.Verbose)]
        public void StorageCapacityReset(string policyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(33, policyName ?? string.Empty, this.ApplicationName);
        }

        [Event(34, Message = "{0} changed storage capacity to {1}", Level = EventLevel.Verbose)]
        public void StorageCapacityChanged(string policyName, int newCapacity, string appDomainName = "Incorrect")
        {
            this.WriteEvent(34, policyName ?? string.Empty, newCapacity, this.ApplicationName);
        }

        [Event(35, Message = "ThrottlingRetryAfterParsedInSec: {0}", Level = EventLevel.Verbose)]
        public void ThrottlingRetryAfterParsedInSec(double retryAfter, string appDomainName = "Incorrect")
        {
            this.WriteEvent(35, retryAfter, this.ApplicationName);
        }

        [Event(36, Message = "TransmitterEmptyStorage", Level = EventLevel.Verbose)]
        public void TransmitterEmptyStorage(string appDomainName = "Incorrect")
        {
            this.WriteEvent(36, this.ApplicationName);
        }

        [Event(37, Message = "TransmitterEmptyBuffer", Level = EventLevel.Verbose)]
        public void TransmitterEmptyBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(37, this.ApplicationName);
        }

        [Event(38, Message = "SubscribeToNetworkFailure: {0}", Level = EventLevel.Warning)]
        public void SubscribeToNetworkFailureWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(38, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(39, Message = "SubscribeToNetworkFailure: {0}", Level = EventLevel.Warning)]
        public void ExceptionHandlerStartExceptionWarning(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(39, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(40, Message = "TransmitterSenderSkipped. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterSenderSkipped(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(40, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(41, Message = "TransmitterBufferSkipped. TransmissionId: {0}. Last backend status code: {1}. Current delay in sec: {2}.", Level = EventLevel.Verbose)]
        public void TransmitterBufferSkipped(string transmissionId, int statusCode, double currentDelayInSeconds, string appDomainName = "Incorrect")
        {
            this.WriteEvent(41, transmissionId, statusCode, currentDelayInSeconds, this.ApplicationName);
        }

        [Event(42, Message = "TransmitterStorageSkipped. TransmissionId: {0}.", Level = EventLevel.Verbose)]
        public void TransmitterStorageSkipped(string transmissionId, string appDomainName = "Incorrect")
        {
            this.WriteEvent(42, transmissionId ?? string.Empty, this.ApplicationName);
        }

        [Event(43, Message = "IncorrectFileFormat. Error: {0}.", Level = EventLevel.Warning)]
        public void IncorrectFileFormatWarning(string errorMessage, string appDomainName = "Incorrect")
        {
            this.WriteEvent(43, errorMessage ?? string.Empty, this.ApplicationName);
        }

        [Event(44, Message = "Unexpected exception when handling IApplicationLifecycle.Stopping event:{0}", Level = EventLevel.Error)]
        public void UnexpectedExceptionInStopError(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(44, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(45, Message = "Transmission polices failed to execute. Exception:{0}", Level = EventLevel.Error)]
        public void ApplyPoliciesError(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(45, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(46, Message = "Retry-After http header: '{0}'. Transmission will be stopped.", Level = EventLevel.Warning)]
        public void RetryAfterHeaderIsPresent(string retryAfterHeader, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                46,
                retryAfterHeader ?? string.Empty,
                this.ApplicationName);
        }

        [Event(48, Message = "TransmissionFailedToStoreWarning. TransmissionId: {0}. Exception: {1}.", Level = EventLevel.Warning)]
        public void TransmissionFailedToStoreWarning(string transmissionId, string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(48, transmissionId ?? string.Empty, exception ?? string.Empty, this.ApplicationName);
        }

        [Event(49, Message = "MovedFromSenderToBuffer.", Level = EventLevel.Verbose)]
        public void MovedFromSenderToBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(49, this.ApplicationName);
        }

        [Event(50, Message = "MovedFromStorageToSender.", Level = EventLevel.Verbose)]
        public void MovedFromStorageToSender(string appDomainName = "Incorrect")
        {
            this.WriteEvent(50, this.ApplicationName);
        }

        [Event(51, Message = "MovedFromStorageToBuffer.", Level = EventLevel.Verbose)]
        public void MovedFromStorageToBuffer(string appDomainName = "Incorrect")
        {
            this.WriteEvent(51, this.ApplicationName);
        }

        [Event(52, Message = "MovedFromBufferToStorage.", Level = EventLevel.Verbose)]
        public void MovedFromBufferToStorage(string appDomainName = "Incorrect")
        {
            this.WriteEvent(52, this.ApplicationName);
        }

        [Event(53, Message = "UnauthorizedAccessExceptionOnCalculateSizeWarning. Message: {0}.", Level = EventLevel.Warning)]
        public void UnauthorizedAccessExceptionOnCalculateSizeWarning(string message, string appDomainName = "Incorrect")
        {
            this.WriteEvent(53, message ?? string.Empty, this.ApplicationName);
        }

        [Event(54, Message = "TransmissionSendingFailed. TransmissionId: {0}. Message: {1}.", Level = EventLevel.Warning)]
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
            Keywords = Keywords.UserActionable,
            Message = "Access to the local storage was denied. If you want Application Insights SDK to store telemetry locally on disk in case of transient network issues please give the process access either to %LOCALAPPDATA% or to %TEMP% folder. After you gave access to the folder you need to restart the process. Currently monitoring will continue but if telemetry cannot be sent it will be dropped. Attempts: {0}.", 
            Level = EventLevel.Error)]
        public void TransmissionStorageAccessDeniedError(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                55,
                error,
                this.ApplicationName);
        }

        [Event(
            56,
            Message = "Access to the local storage was denied. {0}.",
            Level = EventLevel.Warning)]
        public void TransmissionStorageAccessDeniedWarning(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                56,
                error,
                this.ApplicationName);
        }

        [Event(
            57,
            Keywords = Keywords.UserActionable,
            Message = "Server telemetry channel was not initialized. So persistent storage is turned off. You need to call ServerTelemetryChannel.Initialize(). Currently monitoring will continue but if telemetry cannot be sent it will be dropped.",
            Level = EventLevel.Error)]
        public void StorageNotInitializedError(string appDomainName = "Incorrect")
        {
            this.WriteEvent(
                57,
                this.ApplicationName);
        }

        [Event(58, Message = "Sampling skipped manually: {0}.", Level = EventLevel.Verbose)]
        public void SamplingSkippedManually(string appDomainName = "Incorrect")
        {
            this.WriteEvent(58, string.Empty, this.ApplicationName);
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
        }
    }
}
