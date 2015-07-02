namespace System.Runtime.CompilerServices
{
    using System;
    using System.Security;

    internal interface ICriticalNotifyCompletion : INotifyCompletion
    {
        [SecurityCritical]
        void UnsafeOnCompleted(Action continuation);
    }
}
