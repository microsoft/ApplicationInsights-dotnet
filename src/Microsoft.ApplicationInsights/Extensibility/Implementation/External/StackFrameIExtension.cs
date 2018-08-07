namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;

    /// <summary>
    /// Partial class to add the EventData attribute and any additional customizations to the generated type.
    /// </summary>    
    internal partial class StackFrame : IExtension
    {
        public IExtension DeepClone()
        {
            return null;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("level", this.level);
            serializationWriter.WriteProperty("method", this.method);
            serializationWriter.WriteProperty("assembly", this.assembly);
            serializationWriter.WriteProperty("fileName", this.fileName);
            serializationWriter.WriteProperty("line", this.line);
        }
    }
}