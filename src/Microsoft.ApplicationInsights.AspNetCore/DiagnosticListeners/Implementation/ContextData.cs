#if NET451
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
#else
using System.Threading;
#endif

namespace Microsoft.ApplicationInsights.AspNetCore.DiagnosticListeners
{
    internal class ContextData<T>
    {
#if NET451
        private static string Key = typeof(ContextData<T>).FullName;

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
        private readonly AsyncLocal<T> _storage = new AsyncLocal<T>();

        public T Value
        {
            get { return _storage.Value; }
            set { _storage.Value = value; }
        }
#endif
    }
}