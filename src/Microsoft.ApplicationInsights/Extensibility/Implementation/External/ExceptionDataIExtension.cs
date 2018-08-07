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
            serializationWriter.WriteProperty("exceptions", this.exceptions.ToList<IExtension>());
            serializationWriter.WriteProperty("severityLevel", this.severityLevel.TranslateSeverityLevel().HasValue ? this.severityLevel.TranslateSeverityLevel().Value.ToString() : null);

            serializationWriter.WriteProperty("properties", this.properties);
            serializationWriter.WriteProperty("measurements", this.measurements);
        }
    }
}
