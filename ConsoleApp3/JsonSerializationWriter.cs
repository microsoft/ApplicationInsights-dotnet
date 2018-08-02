using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{    
    public class JsonSerializationWriter : ISerializationWriter
    {        
        private readonly TextWriter textWriter;
        private bool currentObjectHasProperties;

        internal JsonSerializationWriter(TextWriter textWriter)
        {            
            this.textWriter = textWriter;
            this.textWriter.Write('{');
        }

        public void Flush()
        {
           this.textWriter.Flush();
        }
        
        // ISerializationWriter
        public void WriteStartObject(string name)
        {
            this.WritePropertyName(name);
            this.textWriter.Write('{');
            this.currentObjectHasProperties = false;
        }

        // ISerializationWriter
        public void WriteProperty(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                this.WritePropertyName(name);
                this.WriteString(value);
            }
        }
        
        public void WriteProperty(string name, int? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void WriteProperty(string name, bool? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value ? "true" : "false");                
            }
        }

        // ISerializationWriter
        public void WriteProperty(string name, double? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        // ISerializationWriter
        public void WriteProperty(string name, TimeSpan? value)
        {
            if (value.HasValue)
            {
                this.WriteProperty(name, value.Value.ToString(string.Empty, CultureInfo.InvariantCulture));                
            }
        }

        public void WriteProperty(string name, DateTimeOffset? value)
        {
            if (value.HasValue)
            {
                this.WriteProperty(name, value.Value.ToString("o", CultureInfo.InvariantCulture));
            }
        }

        public void WriteList(string name, IList<string> items)
        {
            if (items != null && items.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartArray();
                foreach (var item in items)
                {
                    this.WriteComma();
                    this.WriteRawValue(item);
                }
                this.WriteEndArray();

                this.WriteEndObject();
            }
        }

        public void WriteDictionary(string name, IDictionary<string, double> values)
        {
            if (values != null && values.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartObject(name);
                foreach (KeyValuePair<string, double> item in values)
                {
                    this.WriteProperty(item.Key, item.Value);
                }

                this.WriteEndObject();
            }
        }

        public void WriteDictionary(string name, IDictionary<string, string> values)
        {
            if (values != null && values.Count > 0)
            {                
                this.WriteStartObject(name);
                foreach (KeyValuePair<string, string> item in values)
                {
                    this.WriteProperty(item.Key, item.Value);
                }

                this.WriteEndObject();
            }
        }

        public void WriteEndObject()
        {
            this.textWriter.Write('}');
        }


        private void WriteStartArray()
        {
            this.textWriter.Write('[');
        }

        private void WriteEndArray()
        {
            this.textWriter.Write(']');
        }

        private void WriteComma()
        {
            this.textWriter.Write(',');
        }

        private void WriteRawValue(object value)
        {
            this.textWriter.Write(string.Format(CultureInfo.InvariantCulture, "{0}", value));
        }

        /// <summary>
        /// Writes the specified property name enclosed in double quotation marks followed by a colon.
        /// </summary>
        /// <remarks>
        /// When this method is called multiple times, the second call after <see cref="WriteStartObject"/>
        /// and all subsequent calls will write a coma before the name.
        /// </remarks>
        private void WritePropertyName(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0)
            {
                throw new ArgumentException("name");
            }

            if (this.currentObjectHasProperties)
            {
                this.textWriter.Write(',');
            }
            else
            {
                this.currentObjectHasProperties = true;
            }

            this.WriteString(name);
            this.textWriter.Write(':');
        }

        private void WriteString(string value)
        {
            this.textWriter.Write('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        this.textWriter.Write("\\\\");
                        break;
                    case '"':
                        this.textWriter.Write("\\\"");
                        break;
                    case '\n':
                        this.textWriter.Write("\\n");
                        break;
                    case '\b':
                        this.textWriter.Write("\\b");
                        break;
                    case '\f':
                        this.textWriter.Write("\\f");
                        break;
                    case '\r':
                        this.textWriter.Write("\\r");
                        break;
                    case '\t':
                        this.textWriter.Write("\\t");
                        break;
                    default:
                        if (!char.IsControl(c))
                        {
                            this.textWriter.Write(c);
                        }
                        else
                        {
                            this.textWriter.Write(@"\u");
                            this.textWriter.Write(((ushort)c).ToString("x4", CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            this.textWriter.Write('"');
        }

        
    }
}                
    
