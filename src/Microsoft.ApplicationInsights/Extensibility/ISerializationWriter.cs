namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The interface for defining writers capable of serializing data into various formats.
    /// </summary>
    public interface ISerializationWriter
    {
        void WriteProperty(string name, string value);

        void WriteProperty(string name, double? value);

        void WriteProperty(string name, int? value);

        void WriteProperty(string name, bool? value);

        void WriteProperty(string name, TimeSpan? value);

        void WriteList(string name, IList<string> items);

        void WriteDictionary(string name, IDictionary<string, string> items);

        void WriteStartObject(string name);

        void WriteEndObject(string name);
    }
}