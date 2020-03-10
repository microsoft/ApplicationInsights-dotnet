namespace Microsoft.ApplicationInsights.Channel
{
    using System.Threading;
    using System.Threading.Tasks;

    internal static class TaskEx
    {
        /// <summary>
        /// Method to create cancelled task for .NET45.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Canceled task.</returns>
        internal static Task FromCanceled(CancellationToken cancellationToken)
        {
#if NET45
            return Task.Factory.StartNew(() => { }, cancellationToken);
#else
            return Task.FromCanceled(cancellationToken);
#endif
        }

        /// <summary>
        /// Generic method to create cancelled task for .NET45.
        /// </summary>
        /// <typeparam name="TResult">Determines the type of task.</typeparam>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        internal static Task<TResult> FromCanceled<TResult>(CancellationToken cancellationToken)
        {
#if NET45
            return new Task<TResult>(() => { return default(TResult); }, cancellationToken);
#else
            return Task.FromCanceled<TResult>(cancellationToken);
#endif
        }
    }
}
