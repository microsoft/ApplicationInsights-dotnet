namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
#if NET451 || NET46
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Messaging;
#else
    using System.Threading;
#endif

    /// <summary>
    /// Represents ambient data that is local to a given asynchronous control flow, such as an asynchronous method.
    /// </summary>
    /// <typeparam name="T">The type of the ambient data. </typeparam>
    internal class ContextData<T>
    {
#if NET451 || NET46
        private static readonly string Key = typeof(ContextData<T>).FullName;

        /// <summary>
        /// Gets or sets the value of the ambient data.
        /// </summary>
        /// <returns>The value of the ambient data. </returns>
        public T Value
        {
            get
            {
                var handle = CallContext.LogicalGetData(Key) as ObjectHandle;
                return handle != null ? (T)handle.Unwrap() : default(T);
            }

            set
            {
                CallContext.LogicalSetData(Key, new ObjectHandle(value));
            }
        }
#else
        private readonly AsyncLocal<T> storage = new AsyncLocal<T>();

        /// <summary>
        /// Gets or sets the value of the ambient data.
        /// </summary>
        /// <returns>The value of the ambient data. </returns>
        public T Value
        {
            get { return this.storage.Value; }
            set { this.storage.Value = value; }
        }
#endif
    }
}