namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// Interface for defining strongly typed extensions to telemetry types.
    /// </summary>
    public interface IExtension : ISerializableWithWriter, ICloneable
    {
    }
}
