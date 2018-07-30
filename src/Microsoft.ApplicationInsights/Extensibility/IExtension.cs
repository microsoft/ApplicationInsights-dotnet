namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// Interface for defining strongly typed extensions to telemetry types.
    /// </summary>
    public interface IExtension
    {
        /// <summary>
        /// Writes serialization info about the class using the given <see cref="ISerializationWriter"/>
        /// </summary>
        void Serialize(ISerializationWriter serializationWriter);

        /// <summary>
        /// Deep clones the members of the class.
        /// </summary>
        IExtension DeepClone();
    }
}
