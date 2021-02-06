namespace Microsoft.ApplicationInsights.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;

    /// <summary>
    /// Efficient implementation of fetching properties of anonymous types with reflection.
    /// </summary>
    [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "This is used substantially")]
    internal class PropertyFetcher
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
                this.innerFetcher = PropertyFetch.FetcherForProperty(obj?.GetType()?.GetTypeInfo()?.GetDeclaredProperty(this.propertyName));
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
                    this.propertyFetch = (Func<TObject, TProperty>)property.GetMethod.CreateDelegate(typeof(Func<TObject, TProperty>));
                }

                public override object Fetch(object obj)
                {
                    return this.propertyFetch((TObject)obj);
                }
            }
        }
    }
}
