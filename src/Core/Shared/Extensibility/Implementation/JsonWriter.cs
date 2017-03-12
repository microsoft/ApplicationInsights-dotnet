namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Microsoft.ApplicationInsights.DataContracts;

    internal class JsonWriter : IJsonWriter
    {
        private readonly EmptyObjectDetector emptyObjectDetector;
        private readonly TextWriter textWriter;
        private bool currentObjectHasProperties;

        internal JsonWriter(TextWriter textWriter)
        {
            this.emptyObjectDetector = new EmptyObjectDetector();
            this.textWriter = textWriter;
        }

        public void WriteStartArray()
        {
            this.textWriter.Write('[');
        }

        public void WriteStartObject()
        {
            this.textWriter.Write('{');
            this.currentObjectHasProperties = false;
        }

        public void WriteEndArray()
        {
            this.textWriter.Write(']');
        }

        public void WriteEndObject()
        {
            this.textWriter.Write('}');
        }

        public void WriteComma()
        {
            this.textWriter.Write(',');
        }

        public void WriteRawValue(object value)
        {
            this.textWriter.Write(string.Format(CultureInfo.InvariantCulture, "{0}", value));
        }

        public void WriteProperty(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            { 
                this.WritePropertyName(name);
                this.WriteString(value);
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

        public void WriteProperty(string name, int? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void WriteProperty(string name, double? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

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

        public void WriteProperty(string name, IDictionary<string, double> values)
        {
            if (values != null && values.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartObject();
                foreach (KeyValuePair<string, double> item in values)
                {
                    this.WriteProperty(item.Key, item.Value);
                }

                this.WriteEndObject();
            }
        }

        public void WriteProperty(string name, IDictionary<string, string> values)
        {
            if (values != null && values.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartObject();
                foreach (KeyValuePair<string, string> item in values)
                {
                    this.WriteProperty(item.Key, item.Value);
                }

                this.WriteEndObject();
            }
        }

        /// <summary>
        /// Writes the specified property name enclosed in double quotation marks followed by a colon.
        /// </summary>
        /// <remarks>
        /// When this method is called multiple times, the second call after <see cref="WriteStartObject"/>
        /// and all subsequent calls will write a coma before the name.
        /// </remarks>
        public void WritePropertyName(string name)
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
        
        protected void WriteString(string value)
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

        private sealed class EmptyObjectDetector : IJsonWriter
        {
            public bool IsEmpty { get; set; }

            public void WriteStartArray()
            {
            }
            
            public void WriteStartObject()
            {
            }

            public void WriteEndArray()
            {
            }

            public void WriteEndObject()
            {
            }

            public void WriteComma()
            {
            }

            public void WriteProperty(string name, string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, bool? value)
            {
                if (value.HasValue)
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, int? value)
            {
                if (value.HasValue)
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, double? value)
            {
                if (value.HasValue)
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, TimeSpan? value)
            {
                if (value.HasValue)
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, DateTimeOffset? value)
            {
                if (value.HasValue)
                {
                    this.IsEmpty = false;
                }
            }

            public void WriteProperty(string name, IDictionary<string, double> value)
            {
                if (value != null && value.Count > 0)
                {
                    this.IsEmpty = false;
                }                
            }

            public void WriteProperty(string name, IDictionary<string, string> value)
            {
                if (value != null && value.Count > 0)
                {
                    this.IsEmpty = false;
                }
            }

            public void WritePropertyName(string name)
            {
            }

            public void WriteRawValue(object value)
            {
                if (value != null)
                {
                    this.IsEmpty = false;
                }
            }
        }
    }
}
