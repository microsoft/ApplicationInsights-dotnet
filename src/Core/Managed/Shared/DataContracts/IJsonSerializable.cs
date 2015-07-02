namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Represents objects that support serialization to JSON.
    /// </summary>
    public interface IJsonSerializable
    {
        /// <summary>
        /// Writes JSON representation of the object to the specified <paramref name="writer"/>.
        /// </summary>
        void Serialize(IJsonWriter writer);
    }
}