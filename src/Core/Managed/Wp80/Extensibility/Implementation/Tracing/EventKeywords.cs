namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    /// <summary>
    /// EventSource keywords implementation for Silverlight.
    /// </summary>
    [Flags]
    internal enum EventKeywords : long
    {
        None = 0L,
        All = -1L,
        WdiContext = 562949953421312L,
        WdiDiagnostic = 1125899906842624L,
        Sqm = 2251799813685248L,
        AuditFailure = 4503599627370496L,
        AuditSuccess = 9007199254740992L,
        CorrelationHint = AuditFailure,
        EventLogClassic = 36028797018963968L,
    }
}
