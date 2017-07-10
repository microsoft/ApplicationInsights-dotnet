namespace Microsoft.ApplicationInsights.DataContracts
{
    /// <summary>
    /// Indicates that an object support deep cloning.
    /// </summary>
    /// <typeparam name="T">The type of the object that results from the cloning operation.</typeparam>
    public interface IDeepCloneable<out T>
    {
        /// <summary>
        /// Clones the object deeply, so that the original object and its clones share no state 
        /// and can be modified independently.
        /// </summary>
        /// <returns>The cloned object.</returns>
        T DeepClone();
    }
}
