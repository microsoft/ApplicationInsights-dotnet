namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICredentialEnvelope
    {
        /// <summary>
        /// Gets the TokenCredential instance held by this class.
        /// </summary>
        object Credential { get; }

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        string GetToken(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an Azure.Core.AccessToken.
        /// </summary>
        /// <param name="cancellationToken">The System.Threading.CancellationToken to use.</param>
        /// <returns>A valid Azure.Core.AccessToken.</returns>
        Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    }
}
