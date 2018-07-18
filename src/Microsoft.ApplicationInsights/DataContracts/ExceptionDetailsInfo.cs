namespace Microsoft.ApplicationInsights.DataContracts
{
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Wrapper class for <see cref="ExceptionDetails"/> that lets user gets/sets TypeName and Message.
    /// </summary>
    public sealed class ExceptionDetailsInfo
    {
        internal readonly ExceptionDetails InternalExceptionDetails = null;

        /// <summary>
        /// Constructs the 
        /// </summary>
        /// <param name="exceptionDetails">Instance of </param>
        internal ExceptionDetailsInfo(ExceptionDetails exceptionDetails)
        {
            this.InternalExceptionDetails = exceptionDetails;
        }

        /// <summary>
        /// Gets or sets type name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string TypeName
        {
            get => this.InternalExceptionDetails.typeName;
            set => this.InternalExceptionDetails.typeName = value;
        }

        /// <summary>
        /// Gets or sets message name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string Message
        {
            get => this.InternalExceptionDetails.message;
            set => this.InternalExceptionDetails.message = value;
        }
    }
}
