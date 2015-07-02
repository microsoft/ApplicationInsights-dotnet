namespace System
{
    using System.Threading;

    internal static class EnvironmentEx
    {
        public static int CurrentManagedThreadId
        {
            get { return Thread.CurrentThread.ManagedThreadId; }
        }
    }
}
