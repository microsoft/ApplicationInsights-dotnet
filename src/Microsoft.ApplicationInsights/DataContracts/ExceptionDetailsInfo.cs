namespace Microsoft.ApplicationInsights.DataContracts
{
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;

    /// <summary>
    /// Wrapper class for <see cref="ExceptionDetails"/> that lets user gets/sets TypeName and Message.
    /// </summary>
    public sealed class ExceptionDetailsInfo
    {
        private readonly ExceptionDetails internalExceptionDetails = null;

        /// <summary>
        /// Constructs the 
        /// </summary>
        /// <param name="exceptionDetails">Instance of </param>
        internal ExceptionDetailsInfo(ExceptionDetails exceptionDetails)
        {
            this.internalExceptionDetails = exceptionDetails;
        }

        /// <summary>
        /// Gets or sets type name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string TypeName
        {
            get => this.internalExceptionDetails.typeName;
            set => this.internalExceptionDetails.typeName = value;
        }

        /// <summary>
        /// Gets or sets message name of the underlying <see cref="System.Exception"/> that this object represents.
        /// </summary>
        public string Message
        {
            get => this.internalExceptionDetails.message;
            set => this.internalExceptionDetails.message = value;
        }
    }
}
