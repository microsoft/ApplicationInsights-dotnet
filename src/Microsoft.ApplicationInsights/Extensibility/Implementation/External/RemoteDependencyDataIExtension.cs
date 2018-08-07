namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Partial class to implement IExtension
    /// </summary>
    internal partial class RemoteDependencyData : IExtension
    {
        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ver", this.ver);
            serializationWriter.WriteProperty("name", this.name);
            serializationWriter.WriteProperty("id", this.id);
            serializationWriter.WriteProperty("data", this.data);
            serializationWriter.WriteProperty("duration", Utils.ValidateDuration(this.duration));
            serializationWriter.WriteProperty("resultCode", this.resultCode);
            serializationWriter.WriteProperty("success", this.success);
            serializationWriter.WriteProperty("type", this.type);
            serializationWriter.WriteProperty("target", this.target);
            serializationWriter.WriteDictionary("properties", this.properties);
            serializationWriter.WriteDictionary("measurements", this.measurements);
        }

        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }
    }
}