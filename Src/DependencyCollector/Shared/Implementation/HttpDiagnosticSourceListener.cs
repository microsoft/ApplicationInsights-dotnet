#if !NET40
namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;

    /// <summary>
    /// Diagnostic listener implementation that listens for Http DiagnosticSource to see all outgoing HTTP dependency requests.
    /// </summary>
    internal class HttpDiagnosticSourceListener : IObserver<KeyValuePair<string, object>>, IDisposable
    {
        private readonly FrameworkHttpProcessing httpProcessingFramework;
        private readonly HttpDiagnosticSourceSubscriber subscribeHelper;
        private readonly PropertyFetcher requestFetcherRequestEvent;
        private readonly PropertyFetcher requestFetcherResponseEvent;
        private readonly PropertyFetcher responseFetcher;
        private bool disposed = false;

        internal HttpDiagnosticSourceListener(FrameworkHttpProcessing httpProcessing)
        {
            this.httpProcessingFramework = httpProcessing;
            this.subscribeHelper = new HttpDiagnosticSourceSubscriber(this);
            this.requestFetcherRequestEvent = new PropertyFetcher("Request");
            this.requestFetcherResponseEvent = new PropertyFetcher("Request");
            this.responseFetcher = new PropertyFetcher("Response");
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This method gets called once for each event from the Http DiagnosticSource.
        /// </summary>
        /// <param name="value">The pair containing the event name, and an object representing the payload. The payload
        /// is essentially a dynamic object that contain different properties depending on the event.</param>
        public void OnNext(KeyValuePair<string, object> value)
        {
            try
            {
                switch (value.Key)
                {
                    case "System.Net.Http.Desktop.HttpRequestOut.Start": 
                    case "System.Net.Http.Request": // remove in 2.5.0
                    {
                        var request = (HttpWebRequest)this.requestFetcherRequestEvent.Fetch(value.Value);
                        this.httpProcessingFramework.OnRequestSend(request);
                        break;
                    }

                    case "System.Net.Http.Desktop.HttpRequestOut.Stop":
                    case "System.Net.Http.Response": // remove in 2.5.0
                    {
                        var request = (HttpWebRequest)this.requestFetcherResponseEvent.Fetch(value.Value);
                        var response = (HttpWebResponse)this.responseFetcher.Fetch(value.Value);
                        this.httpProcessingFramework.OnResponseReceive(request, response);
                        break;
                    }
                }
            }
            catch (Exception exc)
            {
                DependencyCollectorEventSource.Log.CallbackError(0, "OnNext", exc);
            }
        }

        /// <summary>
        /// Notifies the observer that the provider has finished sending push-based notifications.
        /// <seealso cref="IObserver{T}.OnCompleted()"/>
        /// </summary>
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Notifies the observer that the provider has experienced an error condition.
        /// <seealso cref="IObserver{T}.OnError(Exception)"/>
        /// </summary>
        /// <param name="error">An object that provides additional information about the error.</param>
        public void OnError(Exception error)
        {
        }

        /// <summary>
        /// IDisposable implementation.
        /// </summary>
        /// <param name="disposing">The method has been called directly or indirectly by a user's code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.subscribeHelper != null)
                    {
                        this.subscribeHelper.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        #region PropertyFetcher

        /// <summary>
        /// Efficient implementation of fetching properties of anonymous types with reflection.
        /// </summary>
        private class PropertyFetcher
        {
            private readonly string propertyName;
            private PropertyFetch innerFetcher;

            public PropertyFetcher(string propertyName)
            {
                this.propertyName = propertyName;
            }

            public object Fetch(object obj)
            {
                if (this.innerFetcher == null)
                {
                    this.innerFetcher = PropertyFetch.FetcherForProperty(obj.GetType().GetTypeInfo().GetDeclaredProperty(this.propertyName));
                }

                return this.innerFetcher?.Fetch(obj);
            }

            // see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/DiagnosticSourceEventSource.cs
            private class PropertyFetch
            {
                /// <summary>
                /// Create a property fetcher from a .NET Reflection PropertyInfo class that
                /// represents a property of a particular type.  
                /// </summary>
                public static PropertyFetch FetcherForProperty(PropertyInfo propertyInfo)
                {
                    if (propertyInfo == null)
                    {
                        // returns null on any fetch.
                        return new PropertyFetch(); 
                    }

                    var typedPropertyFetcher = typeof(TypedFetchProperty<,>);
                    var instantiatedTypedPropertyFetcher = typedPropertyFetcher.GetTypeInfo().MakeGenericType(
                        propertyInfo.DeclaringType, propertyInfo.PropertyType);
                    return (PropertyFetch)Activator.CreateInstance(instantiatedTypedPropertyFetcher, propertyInfo);
                }

                /// <summary>
                /// Given an object, fetch the property that this propertyFetch represents. 
                /// </summary>
                public virtual object Fetch(object obj)
                {
                    return null;
                }

                private class TypedFetchProperty<TObject, TProperty> : PropertyFetch
                {
                    private readonly Func<TObject, TProperty> propertyFetch;

                    public TypedFetchProperty(PropertyInfo property)
                    {
                        this.propertyFetch =
                            (Func<TObject, TProperty>)
                            property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
                    }

                    public override object Fetch(object obj)
                    {
                        return this.propertyFetch((TObject)obj);
                    }
                }
            }
        }
        
        #endregion
    }
}
#endif