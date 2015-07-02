namespace System.Runtime.CompilerServices
{
    using System;

    internal interface INotifyCompletion
    {
        void OnCompleted(Action continuation);
    }
}
