namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to implement IExtension
    /// </summary>
    internal partial class PageViewData : IExtension
    {
        public new void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ver", this.ver);
            serializationWriter.WriteProperty("name", this.name);
            serializationWriter.WriteProperty("url", this.url);
            serializationWriter.WriteProperty("duration", this.duration);
            serializationWriter.WriteDictionary("properties", this.properties);
            serializationWriter.WriteDictionary("measurements", this.measurements);
        }

        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }
    }
}