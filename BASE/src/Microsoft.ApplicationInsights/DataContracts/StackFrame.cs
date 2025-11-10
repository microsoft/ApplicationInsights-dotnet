namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Wrapper class for Extensibility.Implementation.External.StackFrame"/> for API exposure.
    /// </summary>
    public sealed class StackFrame
    {
        /// <summary>
        /// Constructs an instance.
        /// </summary>
        public StackFrame(string assembly, string fileName, int level, int line, string method)
        {
            this.Assembly = assembly;
            this.FileName = fileName;
            this.Level = level;
            this.Line = line;
            this.Method = method;
        }

        /// <summary>
        /// Gets or sets the assembly name.
        /// </summary>
        internal string Assembly { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        internal string FileName { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        internal int Level { get; set; }

        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        internal int Line { get; set; }

        /// <summary>
        /// Gets or sets the method name.
        /// </summary>
        internal string Method { get; set; }
    }
}