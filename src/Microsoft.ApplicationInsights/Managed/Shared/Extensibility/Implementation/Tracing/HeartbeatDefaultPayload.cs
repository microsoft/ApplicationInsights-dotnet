namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal static class HeartbeatDefaultPayload
    {
        private static readonly IHeartbeatDefaultPayloadProvider[] DefaultPayloadProviders =
        {
            new BaseHeartbeatProperties(),
            new AzureHeartbeatProperties()
        };

        public static bool IsDefaultKeyword(string keyword)
        {
            foreach (var payloadProvider in DefaultPayloadProviders)
            {
                if (payloadProvider.IsKeyword(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> PopulateDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatProvider provider, bool allowAzureSpecificProperties)
        {
            bool populatedFields = false;

            foreach (var payloadProvider in DefaultPayloadProviders)
            {
                if (payloadProvider is AzureHeartbeatProperties && !allowAzureSpecificProperties)
                {
                    // skip any azure specific modules here
                    continue;
                }

                bool fieldsAreSet = await payloadProvider.SetDefaultPayload(disabledFields, provider).ConfigureAwait(false);
                populatedFields = populatedFields || fieldsAreSet;
            }

            return populatedFields;
        }
    }
}
