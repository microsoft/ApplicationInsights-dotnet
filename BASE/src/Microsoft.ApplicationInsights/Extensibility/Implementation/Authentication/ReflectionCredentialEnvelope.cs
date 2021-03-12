namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Authentication
{
    using System;
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
            if (this.IsTokenCredential(tokenCredential.GetType()))
            {
                this.Credential = tokenCredential;
            }
            else
            {
                throw new ArgumentException($"The provided {nameof(tokenCredential)} must inherit Azure.Core.TokenCredential", nameof(tokenCredential));
            }
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

        private bool IsTokenCredential(Type inputType)
        {
            for (var evalType = inputType; evalType != null; evalType = evalType.BaseType)
            {
                if (evalType.FullName == "Azure.Core.TokenCredential"
                    && evalType.Assembly.FullName.StartsWith("Azure.Core, Version=1."))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
