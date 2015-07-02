namespace System.Runtime.CompilerServices
{
    using System;

    internal interface IAsyncStateMachine
    {
        void MoveNext();

        void SetStateMachine(IAsyncStateMachine stateMachine);
    }
}
