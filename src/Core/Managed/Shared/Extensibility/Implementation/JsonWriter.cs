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

        public void WriteRequiredProperty(string name, string value)
        {
            this.WriteRequiredProperty(name, value, value.Length);
        }

        public void WriteRequiredProperty(string name, string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "n/a";
            }

            this.WritePropertyName(name);
            this.WriteString(value, maxLength);
        }

        public void WriteProperty(string name, string value, int maxLength)
        {
            if (!string.IsNullOrEmpty(value))
            {
                this.WritePropertyName(name);
                this.WriteString(value, maxLength);
            }
        }

        public void WriteProperty(string name, string value)
        {
            this.WriteProperty(name, value, value == null ? 0 : value.Length);
        }

        public void WriteProperty(string name, bool? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value ? "true" : "false");
            }
        }

        public void WriteRequiredProperty(string name, bool value)
        {
            this.WritePropertyName(name);
            this.textWriter.Write(value ? "true" : "false");
        }

        public void WriteRequiredProperty(string name, int value)
        {
            this.WritePropertyName(name);
            this.textWriter.Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public void WriteProperty(string name, int? value)
        {
            if (value.HasValue)
            {
                this.WritePropertyName(name);
                this.textWriter.Write(value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void WriteRequiredProperty(string name, double value)
        {
            this.WritePropertyName(name);
            this.textWriter.Write(value.ToString(CultureInfo.InvariantCulture));
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
#if NET45 || NET46
                this.WriteProperty(name, value.Value.ToString(string.Empty, CultureInfo.InvariantCulture));
#else
                this.WriteProperty(name, value.Value.ToString(CultureInfo.InvariantCulture, string.Empty));
#endif
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
            this.WriteProperty(name, values, int.MaxValue);
        }

        public void WriteProperty(string name, IDictionary<string, double> values, int maxCount)
        {
            if (values != null && values.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartObject();

                int count = 0;
                foreach (KeyValuePair<string, double> item in values)
                {
                    if (count > maxCount)
                    {
                        break;
                    }

                    ++count;

                    this.WriteProperty(item.Key, item.Value);
                }

                this.WriteEndObject();
            }
        }

        public void WriteProperty(string name, IDictionary<string, string> values)
        {
            this.WriteProperty(name, values, int.MaxValue, int.MaxValue);
        }

        public void WriteProperty(string name, IDictionary<string, string> values, int maxKeysCount, int maxValueLength)
        {
            if (values != null && values.Count > 0)
            {
                this.WritePropertyName(name);
                this.WriteStartObject();

                int count = 0;
                foreach (KeyValuePair<string, string> item in values)
                {
                    if (count > maxKeysCount)
                    {
                        break;
                    }
                    ++count;

                    this.WriteProperty(item.Key, item.Value, maxValueLength);
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
            this.WriteString(value, value.Length);
        }

        protected void WriteString(string value, int maxLength)
        {
            this.textWriter.Write('"');

            int count = 0;

            foreach (char c in value)
            {
                if (count > maxLength)
                {
                    break;
                }

                ++count;

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
