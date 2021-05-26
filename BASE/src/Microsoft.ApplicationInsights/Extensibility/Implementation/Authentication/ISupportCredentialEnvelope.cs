namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface defines a class that accepts the <see cref="CredentialEnvelope"/> as a property.
    /// </summary>
    internal interface ISupportCredentialEnvelope
    {
        /// <summary>
        /// Gets or sets the <see cref="CredentialEnvelope"/>.
        /// </summary>
        ReflectionCredentialEnvelope CredentialEnvelope { get; set; }
    }
}
