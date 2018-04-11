namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
#if !NETSTANDARD1_3
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// This exception is used to notify the user that the set of inner exceptions has been trimmed because it exceeded our allowed send limit.
    /// </summary>
#if !NETSTANDARD1_3
    [Serializable]
#endif
    public class InnerExceptionCountExceededException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InnerExceptionCountExceededException"/> class.
        /// </summary>
        public InnerExceptionCountExceededException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerExceptionCountExceededException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error. </param>
        public InnerExceptionCountExceededException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerExceptionCountExceededException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception. </param><param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified. </param>
        public InnerExceptionCountExceededException(string message, Exception innerException) : base(message, innerException)
        {
        }
#if !NETSTANDARD1_3

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerExceptionCountExceededException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="System.Runtime.Serialization.SerializationInfo"/> that holds the serialized object data about the exception being thrown. </param>
        /// <param name="context">The <see cref="System.Runtime.Serialization.StreamingContext"/> that contains contextual information about the source or destination. </param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="info"/> parameter is null. </exception>
        /// <exception cref="System.Runtime.Serialization.SerializationException">The class name is null or <see cref="System.Exception.HResult"/> is zero (0). </exception>
        protected InnerExceptionCountExceededException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}