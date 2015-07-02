namespace System.Runtime.CompilerServices
{
    using System.Threading.Tasks;

    internal static class AsyncTaskCache
    {
        internal const int EXCLUSIVE_INT32_MAX = 9;
        internal static readonly Task<bool> FalseTask = CreateCacheableTask<bool>(false);
        internal const int INCLUSIVE_INT32_MIN = -1;
        internal static readonly Task<int>[] Int32Tasks = CreateInt32Tasks();
        internal static readonly Task<bool> TrueTask = CreateCacheableTask<bool>(true);

        internal static Task<TResult> CreateCacheableTask<TResult>(TResult result)
        {
            return TaskEx.FromResult(result);
        }

        private static Task<int>[] CreateInt32Tasks()
        {
            Task<int>[] taskArray = new Task<int>[10];
            for (int i = 0; i < taskArray.Length; i++)
            {
                taskArray[i] = CreateCacheableTask<int>(i + -1);
            }

            return taskArray;
        }
    }
}
