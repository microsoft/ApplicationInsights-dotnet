namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Collections.Generic;

    /// <summary>
    /// Partial class to implement ISerializableWithWriter. TODO: Remove this
    /// </summary>
    internal partial class RequestData
    {
#pragma warning disable SA1300 // Element must begin with upper-case letter
        public IDictionary<string, double> measurements
        {
            get;
            set;
        }
#pragma warning restore SA1300 // Element must begin with upper-case letter
    }
}