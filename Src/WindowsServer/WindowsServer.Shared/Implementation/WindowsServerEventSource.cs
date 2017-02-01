namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if NET45
    using System.Diagnostics.Tracing;
#endif

    /// <summary>
    /// ETW EventSource tracing class.
    /// </summary>
    [EventSource(Name = "Microsoft-ApplicationInsights-Extensibility-WindowsServer")]
    internal sealed class WindowsServerEventSource : EventSource
    {
        /// <summary>
        /// Instance of the WindowsServerEventSource class.
        /// </summary>
        public static readonly WindowsServerEventSource Log = new WindowsServerEventSource();

        private WindowsServerEventSource()
        {
            this.ApplicationName = this.GetApplicationName();
        }

        public string ApplicationName { [NonEvent]get; [NonEvent]private set; }

        [Event(1, Message = "{0} loaded.", Level = EventLevel.Verbose)]
        public void TelemetryInitializerLoaded(string typeName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(1, typeName ?? string.Empty, this.ApplicationName);
        }

        [Event(2, Message = "[msg=TypeNotFound;{0};]", Level = EventLevel.Verbose)]
        public void TypeExtensionsTypeNotLoaded(string typeName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(2, typeName ?? string.Empty, this.ApplicationName);
        }

        [Event(3, Message = "[msg=AssemblyNotFound;{0};]", Level = EventLevel.Verbose)]
        public void TypeExtensionsAssemblyNotLoaded(string assemblyName, string appDomainName = "Incorrect")
        {
            this.WriteEvent(3, assemblyName ?? string.Empty, this.ApplicationName);
        }

        [Event(
            4,
            Keywords = Keywords.UserActionable,
            Message = "BuildInfo.config file has incorrect xml structure. Context component version will not be populated. Exception: {0}.",
            Level = EventLevel.Error)]
        public void BuildInfoConfigBrokenXmlError(string msg, string appDomainName = "Incorrect")
        {
            this.WriteEvent(4, msg ?? string.Empty, this.ApplicationName);
        }

        [Event(
            5,
            Message = "[msg=BuildInfoConfigLoaded];[path={0}]",
            Level = EventLevel.Verbose)]
        public void BuildInfoConfigLoaded(string path, string appDomainName = "Incorrect")
        {
            this.WriteEvent(5, path ?? string.Empty, this.ApplicationName);
        }

        [Event(
            6,
            Message = "[msg=BuildInfoConfigLoaded];[path={0}]",
            Level = EventLevel.Verbose)]
        public void BuildInfoConfigNotFound(string path, string appDomainName = "Incorrect")
        {
            this.WriteEvent(6, path ?? string.Empty, this.ApplicationName);
        }

        [Event(
            7,
            Message = "[WmiError={0}]",
            Level = EventLevel.Warning)]
        public void DeviceContextWmiFailureWarning(string error, string appDomainName = "Incorrect")
        {
            this.WriteEvent(7, error ?? string.Empty, this.ApplicationName);
        }

        [Event(
            8,
            Message = "[TaskSchedulerOnUnobservedTaskException tracked.]",
            Level = EventLevel.Verbose)]
        public void TaskSchedulerOnUnobservedTaskException(string appDomainName = "Incorrect")
        {
            this.WriteEvent(8, this.ApplicationName);
        }

        [Event(
            9,
            Message = "[CurrentDomainOnUnhandledException tracked.]",
            Level = EventLevel.Verbose)]
        public void CurrentDomainOnUnhandledException(string appDomainName = "Incorrect")
        {
            this.WriteEvent(9, this.ApplicationName);
        }

        [Event(
            10,
            Message = "FirstChance exception statistics callback was called, but exception object is null.",
            Level = EventLevel.Verbose)]
        public void FirstChanceExceptionCallbackExeptionIsNull(string appDomainName = "Incorrect")
        {
            this.WriteEvent(10, this.ApplicationName);
        }

        [Event(
            11,
            Message = "FirstChance exception statistics callback was called.",
            Level = EventLevel.Verbose)]
        public void FirstChanceExceptionCallbackCalled(string appDomainName = "Incorrect")
        {
            this.WriteEvent(11, this.ApplicationName);
        }

        [Event(
            12,
            Message = "FirstChance exception statistics callback failed with the exception {0}.",
            Level = EventLevel.Warning)]
        public void FirstChanceExceptionCallbackException(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(12, exception, this.ApplicationName);
        }

        [Event(
            13,
            Message = "[UnobservedTaskException threw another exception:  {0}.]",
            Level = EventLevel.Error)]
        public void UnobservedTaskExceptionThrewUnhandledException(string exception, string appDomainName = "Incorrect")
        {
            this.WriteEvent(13, exception, this.ApplicationName);
        }

        [NonEvent]
        private string GetApplicationName()
        {
            string name;
            try
            {
                name = AppDomain.CurrentDomain.FriendlyName;
            }
            catch (Exception exp)
            {
                name = "Undefined " + exp;
            }

            return name;
        }

        /// <summary>
        /// Keywords for the PlatformEventSource. Those keywords should match keywords in Core.
        /// </summary>
        public sealed class Keywords
        {
            /// <summary>
            /// Key word for user actionable events.
            /// </summary>
            public const EventKeywords UserActionable = (EventKeywords)0x1;
        }
    }
}
