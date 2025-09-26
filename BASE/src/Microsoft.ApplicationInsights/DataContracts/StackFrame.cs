namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Wrapper class for Extensibility.Implementation.External.StackFrame"/> for API exposure.
    /// </summary>
    public sealed class StackFrame
    {
        // TODO: fix the constructor to set properties

        /// <summary>
        /// Constructs an instance.
        /// </summary>
#pragma warning disable IDE0290 // Use primary constructor
#pragma warning disable CA1801 // Review unused parameters
        public StackFrame(string assembly, string fileName, int level, int line, string method)
#pragma warning restore CA1801 // Review unused parameters
#pragma warning restore IDE0290 // Use primary constructor
        {
        }
    }
}