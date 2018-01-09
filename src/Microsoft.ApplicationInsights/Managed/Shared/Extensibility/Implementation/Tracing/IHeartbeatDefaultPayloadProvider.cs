namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal interface IHeartbeatDefaultPayloadProvider
    {
        bool IsKeyword(string keyword);

        Task<bool> SetDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatProvider provider);
    }
}
