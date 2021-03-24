namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICredentialEnvelope
    {
        object Credential { get; }

        string GetToken(CancellationToken cancellationToken);

        Task<string> GetTokenAsync(CancellationToken cancellationToken);
    }
}
