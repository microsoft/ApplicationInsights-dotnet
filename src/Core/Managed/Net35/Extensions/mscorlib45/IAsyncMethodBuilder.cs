namespace System.Runtime.CompilerServices
{
    internal interface IAsyncMethodBuilder
    {
        void PreBoxInitialization<TStateMachine>(ref TStateMachine stateMachine);

        object ObjectIdForDebugger { get; }
    }
}
