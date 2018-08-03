namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The interface for defining writers capable of serializing data into various formats.    
    /// </summary>
    public interface ISerializationWriter
    {
        /// <summary>
        /// Writes name and value for a string field
        /// </summary>
        void WriteProperty(string name, string value);

        /// <summary>
        /// Writes name and value for a double field
        /// </summary>
        void WriteProperty(string name, double? value);

        /// <summary>
        /// Writes name and value for a int field
        /// </summary>
        void WriteProperty(string name, int? value);

        /// <summary>
        /// Writes name and value for a boolean field
        /// </summary>
        void WriteProperty(string name, bool? value);

        /// <summary>
        /// Writes name and value for a TimeSpan field
        /// </summary>
        void WriteProperty(string name, TimeSpan? value);

        /// <summary>
        /// Writes name and value for a DateTimeOffset field
        /// </summary>
        void WriteProperty(string name, DateTimeOffset? value);

        /// <summary>
        /// Writes name and values for a IList field of string
        /// </summary>
        void WriteList(string name, IList<string> items);

        /// <summary>
        /// Writes name and values for a IList field of objects implementing IExtension
        /// </summary>
        void WriteList(string name, IList<IExtension> items);

        /// <summary>
        /// Writes name and value for a IDictionary field with string,string as key,value
        /// </summary>
        void WriteDictionary(string name, IDictionary<string, string> items);

        /// <summary>
        /// Writes name and value for a IDictionary field with string,string as key,value
        /// </summary>
        void WriteDictionary(string name, IDictionary<string, double> items);

        /// <summary>
        /// Marks beginning of a complex object.
        /// </summary>
        void WriteStartObject();

        /// <summary>
        /// Marks beginning of a complex object.
        /// </summary>
        void WriteStartObject(string name);

        /// <summary>
        /// Marks ending of a complex object.
        /// </summary>
        void WriteEndObject();

        /// <summary>
        /// Starts a list of items.
        /// </summary>
        void WriteStartList(string name);

        /// <summary>
        /// Marks ending of a list of items.
        /// </summary>
        void WriteEndList();
    }
}