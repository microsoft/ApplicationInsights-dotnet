namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// The base interface for defining strongly typed extensions to the telemetry types.
    /// </summary>
    public interface IExtension
    {
        /// <summary>
        /// Sanitizes the properties of the telemetry item based on DP constraints.
        /// </summary>
        void Serialize(ISerializationWriter serializationWriter);

        /// <summary>
        /// Clones the members of the element.
        /// </summary>
        IExtension DeepClone();
    }
}
