namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;


    /// <summary>
    /// Tests for <see cref="JsonSerializationWriter"/>
    /// </summary>
    [TestClass]
    public class JsonSerializationWriterTests
    {
        [TestMethod]
        public void SerializeComplexObject()
        {
            var complexExtension = new ComplexExtension();            
            var mySubSubExtension1 = new MySubSubExtension() { Field3 = "Value1 for field3", Field4 = 100.00 };
            var mySubSubExtension2 = new MySubSubExtension() { Field3 = "Value2 for field3", Field4 = 200.00 };
            var mySubExtension1 = new MySubExtension() { Field1 = "Value1 for field1", Field2 = 100 , MySubSubExtension = mySubSubExtension1 };
            var mySubExtension2 = new MySubExtension() { Field1 = "Value2 for field1", Field2 = 200, MySubSubExtension = mySubSubExtension2 };
            var listExtension = new List<MySubExtension>();
            listExtension.Add(mySubExtension1);
            listExtension.Add(mySubExtension2);

            var listString = new List<string>();
            listString.Add("Item1");
            listString.Add("Item2");
            listString.Add("Item3");

            complexExtension.MyBoolField = true;
            complexExtension.MyDateTimeOffsetField = DateTimeOffset.Now;
            complexExtension.MyDoubleField = 100.10;
            complexExtension.MyIntField = 100;
            complexExtension.MyStringField = "ValueStringField";
            complexExtension.MyTimeSpanField = TimeSpan.FromSeconds(2);
            complexExtension.MySubExtensionField = mySubExtension1;
            complexExtension.MyExtensionListField = listExtension;
            complexExtension.MyStringListField = listString;

            var dicString = new Dictionary<string, string>();
            dicString.Add("Key1", "Value1");
            dicString.Add("Key2", "Value2");
            complexExtension.MyStringDictionaryField = dicString;

            var dicDouble = new Dictionary<string, double>();
            dicDouble.Add("Key1", 1000.000);
            dicDouble.Add("Key2", 2000.000);
            complexExtension.MyDoubleDictionaryField = dicDouble;

            var stringBuilder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                var jsonSerializationWriter = new JsonSerializationWriter(stringWriter);
                jsonSerializationWriter.WriteStartObject();
                complexExtension.Serialize(jsonSerializationWriter);
                jsonSerializationWriter.WriteEndObject();
            }

            string actualJson = stringBuilder.ToString();
            Trace.WriteLine(actualJson);
            
            JObject obj = JsonConvert.DeserializeObject<JObject>(actualJson);
            
            Assert.IsNotNull(actualJson);            
            Assert.AreEqual("ValueStringField", obj["MyStringField"].ToString());
            Assert.AreEqual(100, int.Parse(obj["MyIntField"].ToString()));
            Assert.AreEqual(100.10, double.Parse(obj["MyDoubleField"].ToString()));
            Assert.AreEqual(true, bool.Parse(obj["MyBoolField"].ToString()));
            Assert.AreEqual(TimeSpan.FromSeconds(2), TimeSpan.Parse(obj["MyTimeSpanField"].ToString()));
            //Assert.AreEqual(DateTimeOffset., double.Parse(obj["MyDateTimeOffsetField"].ToString()));

            Assert.AreEqual("Value1 for field1",obj["MySubExtensionField"]["Field1"].ToString());
            Assert.AreEqual(100, int.Parse(obj["MySubExtensionField"]["Field2"].ToString()));

            Assert.AreEqual("Value1 for field3", obj["MySubExtensionField"]["MySubSubExtension"]["Field3"].ToString());
            Assert.AreEqual(100, int.Parse(obj["MySubExtensionField"]["MySubSubExtension"]["Field4"].ToString()));

            Assert.AreEqual("Item1", obj["MyStringListField"][0].ToString());
            Assert.AreEqual("Item2", obj["MyStringListField"][1].ToString());
            Assert.AreEqual("Item3", obj["MyStringListField"][2].ToString());

            Assert.AreEqual("Value1 for field1", obj["MyExtensionListField"][0]["Field1"].ToString());
            Assert.AreEqual(100, int.Parse(obj["MyExtensionListField"][0]["Field2"].ToString()));
            Assert.AreEqual("Value1 for field3", obj["MyExtensionListField"][0]["MySubSubExtension"]["Field3"].ToString());
            Assert.AreEqual(100, int.Parse(obj["MyExtensionListField"][0]["MySubSubExtension"]["Field4"].ToString()));

            Assert.AreEqual("Value2 for field1", obj["MyExtensionListField"][1]["Field1"].ToString());
            Assert.AreEqual(200, int.Parse(obj["MyExtensionListField"][1]["Field2"].ToString()));
            Assert.AreEqual("Value2 for field3", obj["MyExtensionListField"][1]["MySubSubExtension"]["Field3"].ToString());
            Assert.AreEqual(200, int.Parse(obj["MyExtensionListField"][1]["MySubSubExtension"]["Field4"].ToString()));

            Assert.AreEqual("Value1", obj["MyStringDictionaryField"]["Key1"].ToString());
            Assert.AreEqual("Value2", obj["MyStringDictionaryField"]["Key2"].ToString());

            Assert.AreEqual(1000, double.Parse(obj["MyDoubleDictionaryField"]["Key1"].ToString()));
            Assert.AreEqual(2000, double.Parse(obj["MyDoubleDictionaryField"]["Key2"].ToString()));

        }
    }

    public class ComplexExtension : IExtension
    {
        public string MyStringField;
        public int MyIntField;
        public double MyDoubleField;
        public bool MyBoolField;
        public TimeSpan MyTimeSpanField;
        public DateTimeOffset MyDateTimeOffsetField;
        public MySubExtension MySubExtensionField;
        public IList<string> MyStringListField;
        public IList<MySubExtension> MyExtensionListField;
        public IDictionary<string, string> MyStringDictionaryField;
        public IDictionary<string, double> MyDoubleDictionaryField;
        
        public IExtension DeepClone()
        {
            ComplexExtension other = new ComplexExtension();
            other.MyStringField = this.MyStringField;
            other.MyIntField = this.MyIntField;
            other.MyDoubleField = this.MyDoubleField;
            other.MyBoolField = this.MyBoolField;
            other.MyTimeSpanField = this.MyTimeSpanField;
            other.MyDateTimeOffsetField = this.MyDateTimeOffsetField;
            other.MySubExtensionField = (MySubExtension) this.MySubExtensionField.DeepClone();

            IList<string> otherString = new List<string>();
            foreach(var item in this.MyStringListField)
            {
                otherString.Add(item);
            }
            other.MyStringListField = otherString;

            IList<MySubExtension> others = new List<MySubExtension>();
            foreach(var item in this.MyExtensionListField)
            {
                others.Add((MySubExtension) item.DeepClone());
            }
            other.MyExtensionListField = others;

            IDictionary<string, string> otherStringDic = new Dictionary<string, string>();
            foreach (var item in this.MyStringDictionaryField)
            {
                otherStringDic.Add(item.Key, item.Value);
            }
            other.MyStringDictionaryField = otherStringDic;

            IDictionary<string, double> otherDoubleDic = new Dictionary<string, double>();
            foreach (var item in this.MyDoubleDictionaryField)
            {
                otherDoubleDic.Add(item.Key, item.Value);
            }
            other.MyDoubleDictionaryField = otherDoubleDic;

            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {            
            serializationWriter.WriteProperty("MyStringField", MyStringField);
            serializationWriter.WriteProperty("MyIntField", MyIntField);
            serializationWriter.WriteProperty("MyDoubleField", MyDoubleField);
            serializationWriter.WriteProperty("MyBoolField", MyBoolField);
            serializationWriter.WriteProperty("MyTimeSpanField", MyTimeSpanField);
            serializationWriter.WriteProperty("MyDateTimeOffsetField", MyDateTimeOffsetField);
            serializationWriter.WriteProperty("MySubExtensionField", MySubExtensionField);
            serializationWriter.WriteProperty("MyStringListField", MyStringListField);
            serializationWriter.WriteProperty("MyExtensionListField", MyExtensionListField.ToList<IExtension>());
            serializationWriter.WriteProperty("MyStringDictionaryField", MyStringDictionaryField);
            serializationWriter.WriteProperty("MyDoubleDictionaryField", MyDoubleDictionaryField);            
        }
    }

    public class MySubExtension : IExtension
    {
        public string Field1;
        public int Field2;
        public IExtension MySubSubExtension;

        public IExtension DeepClone()
        {
            MySubExtension other = new MySubExtension();
            other.Field1 = this.Field1;
            other.Field2 = this.Field2;
            other.MySubSubExtension = this.MySubSubExtension.DeepClone();
            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("Field1", Field1);
            serializationWriter.WriteProperty("Field2", Field2);
            serializationWriter.WriteProperty("MySubSubExtension", MySubSubExtension);
        }
    }

    public class MySubSubExtension : IExtension
    {
        public string Field3;
        public double Field4;

        public IExtension DeepClone()
        {
            MySubSubExtension other = new MySubSubExtension();
            other.Field3 = this.Field3;
            other.Field4 = this.Field4;
            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("Field3", Field3);
            serializationWriter.WriteProperty("Field4", Field4);
        }
    }
}