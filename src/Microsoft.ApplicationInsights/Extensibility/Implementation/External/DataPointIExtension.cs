namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to implement IExtension
    /// </summary>
    internal partial class DataPoint : IExtension
    {       
        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ns", this.ns);
            serializationWriter.WriteProperty("name", this.name);
            serializationWriter.WriteProperty("kind", this.kind.ToString());
            serializationWriter.WriteProperty("value", this.value);
            serializationWriter.WriteProperty("count", this.count);
            serializationWriter.WriteProperty("min", this.min);
            serializationWriter.WriteProperty("max", this.max);
            serializationWriter.WriteProperty("stdDev", this.stdDev);
        }

        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }
    }
}
