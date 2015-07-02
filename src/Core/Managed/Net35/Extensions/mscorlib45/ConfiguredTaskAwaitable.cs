namespace System.Runtime.CompilerServices
{
    using System;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading.Tasks;

    [StructLayout(LayoutKind.Sequential)]
    internal struct ConfiguredTaskAwaitable
    {
        private readonly ConfiguredTaskAwaiter m_configuredTaskAwaiter;

        internal ConfiguredTaskAwaitable(Task task, bool continueOnCapturedContext)
        {
            this.m_configuredTaskAwaiter = new ConfiguredTaskAwaiter(task, continueOnCapturedContext);
        }

        public ConfiguredTaskAwaiter GetAwaiter()
        {
            return this.m_configuredTaskAwaiter;
        }
    }
}
