#if NET452 || NET46
namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Our SDK currently targets net452, net46, and netstandard2.0.
    /// Azure.Core.TokenCredential is only available for netstandard2.0.
    /// I'm introducing this class as a wrapper so we can receive an instance of this class and pass it around within our SDK.
    /// </remarks>
    public class ReflectionCredentialEnvelope : ICredentialEnvelope
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Must use reflection to verify that this object is an instance of Azure.Core.TokenCredential.
        /// </remarks>
        /// <param name="tokenCredential"></param>
        public ReflectionCredentialEnvelope(object tokenCredential)
        {
            // TODO: VERIFY TYPE USING REFLECTION
        }

        public object Credential { get; private set; }

        public string GetToken()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTokenAsync()
        {
            throw new NotImplementedException();
        }
    }
}
#endif