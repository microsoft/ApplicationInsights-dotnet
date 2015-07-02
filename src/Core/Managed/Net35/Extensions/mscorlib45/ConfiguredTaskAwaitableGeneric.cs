namespace System.Runtime.CompilerServices
{
    using System;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading.Tasks;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ConfiguredTaskAwaitable<TType>
    {
        private readonly ConfiguredTaskAwaiter<TType> m_configuredTaskAwaiter;

        internal ConfiguredTaskAwaitable(Task<TType> task, bool continueOnCapturedContext)
        {
            this.m_configuredTaskAwaiter = new ConfiguredTaskAwaiter<TType>(task, continueOnCapturedContext);
        }

        public ConfiguredTaskAwaiter<TType> GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }
}
