namespace Microsoft.ApplicationInsights.EventSourceListener.EtwCollector
{
    using System;
    using Diagnostics.Tracing.Session;
    using Microsoft.Diagnostics.Tracing;

    internal interface ITraceEventSession : IDisposable
    {
        /// <summary>
        /// Is the current process Elevated (allowed to turn on a ETW provider). This is
        /// useful because you need to be elevated to enable providers on a TraceEventSession.
        /// </summary>
        bool? IsElevated();

        /// <summary>
        /// Once started, event sessions will persist even after the process that created
        /// them dies. They will also be implicitly stopped when the TraceEventSession is
        /// closed unless the StopOnDispose property is set to false. This API is OK to call
        /// from one thread while Process() is being run on another
        /// </summary>
        bool Stop(bool noThrow = false);

        /// <summary>
        /// If this is a real time session you can fetch the source associated with the session
        /// to start receiving events. Currently does not work on file based sources (we
        /// expect you to wait until the file is complete).
        /// </summary>
        ETWTraceEventSource Source { get; }

        /// <summary>
        /// Enable a NON-KERNEL provider (see also EnableKernelProvider) which has a given
        /// provider name. This API first checks if a published provider exists by that name,
        /// otherwise it assumes it is an EventSouce and determines the provider Guid by
        /// hashing the name according to a well known algorithm. Thus it will never return
        /// a failure for a incorrect spelling of the name.
        /// </summary>
        /// <param name="providerName">
        /// The name of the provider. It must either be registered with the operating system
        /// (logman query providers returns it) or it must be an EventSource (see GetEventSourceGuidFromName)
        /// </param>
        /// <param name="providerLevel">
        /// The verbosity to turn on
        /// </param>
        /// <param name="matchAnyKeywords">
        /// A bitvector representing the areas to turn on. Only the low 32 bits are used
        /// by classic providers and passed as the 'flags' value. Zero is a special value
        /// which is a provider defined default, which is usually 'everything'
        /// </param>
        /// <param name="options">
        /// Additional options for the provider (e.g. taking a stack trace), arguments ...
        /// </param>
        /// <returns>true if the session already existed and needed to be restarted.</returns>
        bool EnableProvider(string providerName, TraceEventLevel providerLevel = TraceEventLevel.Verbose, ulong matchAnyKeywords = ulong.MaxValue, TraceEventProviderOptions options = null);

        /// <summary>
        /// Enable a NON-KERNEL provider (see also EnableKernelProvider) which has a given
        /// provider Guid.
        /// </summary>
        /// <param name="providerGuid">The Guid that represents the event provider enable.</param>
        /// <param name="providerLevel">The verbosity to turn on</param>
        /// <param name="matchAnyKeywords">
        /// A bitvector representing the areas to turn on. Only the low 32 bits are used
        /// by classic providers and passed as the 'flags' value. Zero is a special value
        /// which is a provider defined default, which is usually 'everything'
        /// </param>
        /// <param name="options">Additional options for the provider (e.g. taking a stack trace), arguments ...</param>
        /// <returns>true if the session already existed and needed to be restarted.</returns>
        bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel = TraceEventLevel.Verbose, ulong matchAnyKeywords = ulong.MaxValue, TraceEventProviderOptions options = null);

        /// <summary>
        /// Disables a provider with the given name completely
        /// </summary>
        void DisableProvider(string providerName);


        /// <summary>
        /// Disables a provider with the given provider ID completely
        /// </summary>
        void DisableProvider(Guid providerGuid);
    }
}
