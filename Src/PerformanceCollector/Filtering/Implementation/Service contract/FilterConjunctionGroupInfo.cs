namespace Microsoft.ApplicationInsights.Extensibility.Filtering
{
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// An AND-connected group of FilterInfo objects.
    /// </summary>
    [DataContract]
    internal class FilterConjunctionGroupInfo
    {
        [DataMember]
        public FilterInfo[] Filters { get; set; }

        public override string ToString()
        {
            return string.Join(", ", (this.Filters ?? new FilterInfo[0]).Select(filter => filter.ToString()));
        }
    }
}