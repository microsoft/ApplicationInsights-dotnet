namespace Microsoft.ApplicationInsights.Extensibility
{
    /// <summary>
    /// Interface for defining method to Deeply clone the members.
    /// </summary>
    public interface ICloneable
    {
        /// <summary>
        /// Deep clones the members of the class.
        /// </summary>
        IExtension DeepClone();
    }
}
