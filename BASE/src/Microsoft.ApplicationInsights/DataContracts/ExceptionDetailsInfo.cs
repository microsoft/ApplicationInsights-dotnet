namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Generic;

    /// <summary>
    /// Wrapper class for ExceptionDetails"/> that lets user gets/sets TypeName and Message.
    /// </summary>
    public sealed class ExceptionDetailsInfo
    {
        /// <summary>
        /// Constructs the instance of <see cref="ExceptionDetailsInfo"/>.
        /// </summary>
        /// <param name="id">Exception id.</param>
        /// <param name="outerId">Parent exception's id.</param>
        /// <param name="typeName">Type name for the exception.</param>
        /// <param name="message">Exception message.</param>
        /// <param name="hasFullStack">Indicates that this exception has full stack information.</param>
        /// <param name="stack">Exception's stack trace.</param>
        /// <param name="parsedStack">Exception's stack.</param>
        public ExceptionDetailsInfo(int id, int outerId, string typeName, string message, bool hasFullStack,
            string stack, IEnumerable<StackFrame> parsedStack)
        {
            this.TypeName = typeName;
            this.Message = message;
        }

        /// <summary>
        /// Gets or sets type name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets message name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string Message { get; set; }
    }
}