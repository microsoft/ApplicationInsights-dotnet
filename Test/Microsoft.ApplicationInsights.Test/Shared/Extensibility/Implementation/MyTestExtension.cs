using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    public class MyTestExtension : IExtension
    {
        public int myIntField;
        public string myStringField;

        public IExtension DeepClone()
        {
            var other = new MyTestExtension();
            other.myIntField = this.myIntField;
            other.myStringField = this.myStringField;

            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("myIntField", myIntField);
            serializationWriter.WriteProperty("myStringField", myStringField);
        }
    }
}
