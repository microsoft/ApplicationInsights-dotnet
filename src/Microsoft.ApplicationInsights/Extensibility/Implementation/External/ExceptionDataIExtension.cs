namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Additional implementation for ExceptionDetails.
    /// </summary>
    internal partial class ExceptionData : IExtension
    {
        IExtension IExtension.DeepClone()
        {
            return this.DeepClone();
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("ver", this.ver);
            serializationWriter.WriteProperty("problemId", this.problemId);
            serializationWriter.WriteList("exceptions", this.exceptions.ToList<IExtension>());
            serializationWriter.WriteProperty("severityLevel", this.severityLevel.HasValue ? this.severityLevel.Value.ToString() : null);

            serializationWriter.WriteDictionary("properties", this.properties);
            serializationWriter.WriteDictionary("measurements", this.measurements);
        }
    }
}
