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
        public void SerializeAsStringMethodSerializesATelemetryCorrectly()
        {
            var dbExtension = new DatabaseExtension();
            dbExtension.DatabaseId = 10908;            
            dbExtension.DatabaseServer = "azpacalbcluster011";

            dbExtension.DBLocation = new DBLocation() { city = "Bellevue", state = "WA" };

            var cons = new List<DBConnection>();
            cons.Add(new DBConnection() { userid = "user1", password = "usersecret1" });
            cons.Add(new DBConnection() { userid = "user2", password = "usersecret2" });
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
            
            JObject obj = JsonConvert.DeserializeObject<JObject>(actualJson);
            
            Assert.IsNotNull(actualJson);            
            Assert.AreEqual("10908", obj["DatabaseId"].ToString());
            Assert.AreEqual("azpacalbcluster011", obj["DatabaseServer"].ToString());

            Assert.AreEqual("Bellevue", obj["DBLocation"]["city"].ToString());
            Assert.AreEqual("WA", obj["DBLocation"]["state"].ToString());

            Assert.AreEqual("user1", obj["Connections"][0]["userid"].ToString());
            Assert.AreEqual("usersecret1", obj["Connections"][0]["password"].ToString());

            Assert.AreEqual("user2", obj["Connections"][1]["userid"].ToString());
            Assert.AreEqual("usersecret2", obj["Connections"][1]["password"].ToString());
        }

        [TestMethod]
        public void SerializeAsStringMethodSerializesExceptionCorrectly()
        {

            ExceptionTelemetry myex = null;
            try
            {
                int x = 0;
                int y = 10 / x;
            }
            catch (Exception ex)
            {
                myex = new ExceptionTelemetry(ex);
            }

            var stringBuilder = new StringBuilder();
            using (StringWriter stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                var jsonSerializationWriter = new JsonSerializationWriter(stringWriter);
                jsonSerializationWriter.WriteStartObject();
                myex.Serialize(jsonSerializationWriter);
                jsonSerializationWriter.WriteEndObject();
            }           
            string actualJson = stringBuilder.ToString();
            Trace.WriteLine(actualJson);

            JObject obj = JsonConvert.DeserializeObject<JObject>(actualJson);

            Assert.IsNotNull(actualJson);
        }
    }

    public class DatabaseExtension : IExtension
    {
        public int DatabaseId;
        public string DatabaseServer;
        public IList<DBConnection> Connections;
        public DBLocation DBLocation;

        public IExtension DeepClone()
        {
            DatabaseExtension other = new DatabaseExtension();
            other.DatabaseId = this.DatabaseId;
            other.DatabaseServer = this.DatabaseServer;
            other.DBLocation = (DBLocation) this.DBLocation.DeepClone();
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
            serializationWriter.WriteProperty("DBLocation", DBLocation);
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

    public class DBLocation : IExtension
    {
        public string city;
        public string state;

        public IExtension DeepClone()
        {
            DBLocation other = new DBLocation();
            other.city = this.city;
            other.state = this.state;
            return other;
        }

        public void Serialize(ISerializationWriter serializationWriter)
        {
            serializationWriter.WriteProperty("city", city);
            serializationWriter.WriteProperty("state", state);
        }
    }
}