namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    /// <summary>
    /// Partial class to implement IExtension
    /// </summary>
    internal partial class PageViewPerfData : IExtension
    {
        public new void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ver", this.ver);
            serializationWriter.WriteProperty("name", this.name);
            serializationWriter.WriteProperty("url", this.url);
            serializationWriter.WriteProperty("duration", this.duration);
            serializationWriter.WriteProperty("domProcessing", this.domProcessing);
            serializationWriter.WriteProperty("perfTotal", this.perfTotal);
            serializationWriter.WriteProperty("networkConnect", this.networkConnect);
            serializationWriter.WriteProperty("sentRequest", this.sentRequest);
            serializationWriter.WriteProperty("receivedResponse", this.receivedResponse);
            serializationWriter.WriteDictionary("properties", this.properties);
            serializationWriter.WriteDictionary("measurements", this.measurements);
        }

        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }
    }
}