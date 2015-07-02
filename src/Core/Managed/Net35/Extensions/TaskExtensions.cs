namespace System.Threading.Tasks
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    internal static class TaskExtensionsEx
    {
        public static ConfiguredTaskAwaitable ConfigureAwait(this Task task, bool captureContext)
        {
            return new ConfiguredTaskAwaitable(task, captureContext);
        }

        public static ConfiguredTaskAwaitable<TResult> ConfigureAwait<TResult>(this Task<TResult> task, bool captureContext)
        {
            return new ConfiguredTaskAwaitable<TResult>(task, captureContext);
        }
    }
}