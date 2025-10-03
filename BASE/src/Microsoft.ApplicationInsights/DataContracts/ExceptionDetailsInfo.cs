namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Wrapper class for ExceptionDetails"/> that lets user gets/sets TypeName and Message.
    /// </summary>
    public sealed class ExceptionDetailsInfo
    {
        // TODO : fix the constructor to set properties

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
#pragma warning disable CA1801 // Review unused parameters
        public ExceptionDetailsInfo(int id, int outerId, string typeName, string message, bool hasFullStack,
            string stack, IEnumerable<StackFrame> parsedStack)
#pragma warning restore CA1801 // Review unused parameters
        {
        }

        /// <summary>
        /// Gets or sets type name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string TypeName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets message name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string Message
        {
            get;
            set;
        }
    }
}