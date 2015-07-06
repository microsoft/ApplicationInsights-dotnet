namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
#if WINRT
    using Windows.Foundation.Metadata;
#endif

    /// <summary>
    /// Encapsulates logic for serializing objects to JSON. 
    /// </summary>
    /// <seealso cref="IJsonSerializable"/>.
    public interface IJsonWriter
    {
        /// <summary>
        /// Writes opening/left square bracket.
        /// </summary>
        void WriteStartArray();

        /// <summary>
        /// Writes opening/left curly brace.
        /// </summary>
        void WriteStartObject();

        /// <summary>
        /// Writes closing/right square bracket.
        /// </summary>
        void WriteEndArray();

        /// <summary>
        /// Writes closing/right curly brace.
        /// </summary>
        void WriteEndObject();

        /// <summary>
        /// Writes comma.
        /// </summary>
        void WriteComma();

        /// <summary>
        /// Writes a <see cref="String"/> property.
        /// </summary>
#if WINRT
        [DefaultOverload]
#endif
        void WriteProperty(string name, string value);

        /// <summary>
        /// Writes a <see cref="String"/> property. Will write empty string in case value is null or empty.
        /// </summary>
        void WriteRequiredProperty(string name, string value);

        /// <summary>
        /// Writes a <see cref="Boolean"/> property.
        /// </summary>
        void WriteProperty(string name, bool? value);

        /// <summary>
        /// Writes a required <see cref="Boolean"/> property.
        /// </summary>
        void WriteRequiredProperty(string name, bool value);

        /// <summary>
        /// Writes a <see cref="Int32"/> property.
        /// </summary>
        void WriteProperty(string name, int? value);

        /// <summary>
        /// Writes a required <see cref="Int32"/> property.
        /// </summary>
        void WriteRequiredProperty(string name, int value);

        /// <summary>
        /// Writes a <see cref="Double"/> property.
        /// </summary>
        void WriteProperty(string name, double? value);

        /// <summary>
        /// Writes a <see cref="TimeSpan"/> property.
        /// </summary>
        void WriteProperty(string name, TimeSpan? value);

        /// <summary>
        /// Writes a required <see cref="TimeSpan"/> property.
        /// </summary>
        void WriteRequiredProperty(string name, TimeSpan value);

        /// <summary>
        /// Writes a <see cref="DateTimeOffset"/> property.
        /// </summary>
        void WriteProperty(string name, DateTimeOffset? value);

        /// <summary>
        /// Writes a required <see cref="DateTimeOffset"/> property.
        /// </summary>
        void WriteRequiredProperty(string name, DateTimeOffset value);

        /// <summary>
        /// Writes a <see cref="IDictionary{String, Double}"/> property.
        /// </summary>
        void WriteProperty(string name, IDictionary<string, double> values);

        /// <summary>
        /// Writes a <see cref="IDictionary{String, String}"/> property.
        /// </summary>
        void WriteProperty(string name, IDictionary<string, string> values);

        /// <summary>
        /// Writes an <see cref="IJsonSerializable"/> object property.
        /// </summary>
        void WriteProperty(string name, IJsonSerializable value);

        /// <summary>
        /// Writes a property name in double quotation marks, followed by a colon.
        /// </summary>
        void WritePropertyName(string name);

        /// <summary>
        /// Writes <see cref="Object"/> as raw value directly.
        /// </summary>
        void WriteRawValue(object value);
    }
}
