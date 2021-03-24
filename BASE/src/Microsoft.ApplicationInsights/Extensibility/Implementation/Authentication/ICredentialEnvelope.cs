namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICredentialEnvelope
    {
        object Credential { get; }

        Task<string> GetTokenAsync(CancellationToken cancellationToken);
    }
}
