
namespace StressTest
{
    using System;

    using Microsoft.Diagnostics.OperationTracking;

    internal class NopListener : IOperationStartedEventListener, IOperationSucceededEventListener, IOperationFailedEventListener
    {
        public void Started(long elapsedTicks, Operation operation)
        {
        }

        public void Succeeded(long elapsedTicks, Operation operation)
        {
        }

        public void Failed(long elapsedTicks, Operation operation, ulong errorCode)
        {
        }

        public void Failed(long elapsedTicks, Operation operation, Exception exception)
        {
        }
    }
}
