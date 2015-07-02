namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
#if WINRT
    using TaskEx = System.Threading.Tasks.Task;
#endif

    internal class StubTransmission : Transmission
    {
        public Action<Stream> OnSave = stream => { };

        public Action OnSend = () => { };

        public StubTransmission()
            : base(new Uri("any://uri"), new byte[0], string.Empty, string.Empty)
        {
        }

        public StubTransmission(byte[] content)
            : base(new Uri("any://uri"), content, string.Empty, string.Empty)
        {
        }
    }
}
