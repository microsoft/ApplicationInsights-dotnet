namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System;
    using System.Linq;

    /// <summary>
    /// Additional implementation for ExceptionDetails.
    /// </summary>
    internal partial class ExceptionDetails : IExtension
    {
        public IExtension DeepClone()
        {
            return null;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("id", this.id);
            serializationWriter.WriteProperty("outerId", this.outerId);
            serializationWriter.WriteProperty("typeName", this.typeName);
            serializationWriter.WriteProperty("message", this.message);
            serializationWriter.WriteProperty("hasFullStack", this.hasFullStack);
            serializationWriter.WriteProperty("stack", this.stack);            
            serializationWriter.WriteList("parsedStack", this.parsedStack.ToList<IExtension>());
        }
    }
}