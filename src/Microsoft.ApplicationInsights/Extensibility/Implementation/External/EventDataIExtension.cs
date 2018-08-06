namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to implement IExtension
    /// </summary>
    internal partial class EventData : IExtension
    {
        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ver", this.ver);
            serializationWriter.WriteProperty("name", this.name);

            serializationWriter.WriteDictionary("properties", this.properties);
            serializationWriter.WriteDictionary("measurements", this.measurements);
        }

        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }
    }
}
