namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
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
        public void SerializeAsStringMethodSerializesATelemetryCorrectly()
        {
            var dbExtension = new DatabaseExtension();
            dbExtension.DatabaseId = 10908;
            dbExtension.DatabaseLocation = "Virginia";
            dbExtension.DatabaseServer = "azpacalbcluster011";
            var cons = new List<DBConnection>();
            cons.Add(new DBConnection() { userid = "cijo", password = "secretcijo" });
            cons.Add(new DBConnection() { userid = "anu", password = "secretanu" });
            dbExtension.Connections = cons;


            var stringBuilder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                var jsonSerializationWriter = new JsonSerializationWriter(stringWriter);
                jsonSerializationWriter.WriteStartObject();
                dbExtension.Serialize(jsonSerializationWriter);
                jsonSerializationWriter.WriteEndObject();
            }

            string actualJson = stringBuilder.ToString();
            Trace.WriteLine(actualJson);

            // Expected: {"name":"Microsoft.ApplicationInsights.Exception","time":"0001-01-01T00:00:00.0000000+00:00","data":{"baseType":"ExceptionData","baseData":{"ver":2,"handledAt":"Unhandled","exceptions":[]}}}
            // Deserialize (Validates a valid JSON string)
            JObject obj = JsonConvert.DeserializeObject<JObject>(actualJson);

            // Validates 2 random properties
            Assert.IsNotNull(actualJson);
            
            Assert.AreEqual("10908", obj["DatabaseId"].ToString());
        }       
    }

    public class DatabaseExtension : IExtension
    {
        public int DatabaseId;
        public string DatabaseServer;
        public string DatabaseLocation;
        public IList<DBConnection> Connections;

        public IExtension DeepClone()
        {
            DatabaseExtension other = new DatabaseExtension();
            other.DatabaseId = this.DatabaseId;
            other.DatabaseServer = this.DatabaseServer;
            other.DatabaseLocation = this.DatabaseLocation;
            IList<DBConnection> others = new List<DBConnection>();
            foreach(var item in this.Connections)
            {
                others.Add((DBConnection) item.DeepClone());
            }

            other.Connections = others;
            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {            
            serializationWriter.WriteProperty("DatabaseId", DatabaseId);
            serializationWriter.WriteProperty("DatabaseServer", DatabaseServer);
            serializationWriter.WriteProperty("DatabaseLocation", DatabaseLocation);
            serializationWriter.WriteList("Connections", Connections.ToList<IExtension>());            
        }
    }

    public class DBConnection : IExtension
    {
        public string userid;
        public string password;

        public IExtension DeepClone()
        {
            DBConnection other = new DBConnection();
            other.userid = this.userid;
            other.password = this.password;
            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("userid", userid);
            serializationWriter.WriteProperty("password", password);
        }
    }
}