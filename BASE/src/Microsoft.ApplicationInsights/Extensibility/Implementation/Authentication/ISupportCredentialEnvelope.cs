namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    /// <summary>
    /// This interface defines a class that accepts the <see cref="ICredentialEnvelope"/> as a property.
    /// </summary>
    internal interface ISupportCredentialEnvelope
    {
        /// <summary>
        /// Gets or sets the <see cref="ICredentialEnvelope"/>.
        /// </summary>
        ICredentialEnvelope CredentialEnvelope { get; set; }
    }
}
