namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Contains values that identify state of a user session.
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// Indicates that a user session started.
        /// </summary>
        Start,

        /// <summary>
        /// Indicates that a user session ended.
        /// </summary>
        End
    }
}
