namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System.Threading.Tasks;

    public interface ICredentialEnvelope
    {
        object Credential { get; }

        string GetToken();

        Task<string> GetTokenAsync();
    }
}
