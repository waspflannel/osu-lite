// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions.ExceptionExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Logging;

namespace osu.Game.Extensions
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Denote a task which is to be run without local error handling logic, where failure is not catastrophic.
        /// Avoids unobserved exceptions from being fired.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <param name="onSuccess">An optional action to run on success.</param>
        /// <param name="onError">An optional action to run on error.</param>
        public static void FireAndForget(this Task task, Action? onSuccess = null, Action<Exception>? onError = null) =>
            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.Assert(t.Exception != null);
                    Exception exception = t.Exception.AsSingular();

                    onError?.Invoke(exception);

                    Logger.Error(exception, $"Unobserved exception occurred via {nameof(FireAndForget)} call: {exception.Message}");
                }
                else
                {
                    onSuccess?.Invoke();
                }
            });

        /// <summary>
        /// Add a continuation to be performed only after the attached task has completed.
        /// </summary>
        /// <param name="task">The previous task to be awaited on.</param>
        /// <param name="action">The action to run.</param>
        /// <param name="cancellationToken">An optional cancellation token. Will only cancel the provided action, not the sequence.</param>
        /// <returns>A task representing the provided action.</returns>
        public static Task ContinueWithSequential(this Task task, Action action, CancellationToken cancellationToken = default) =>
            task.ContinueWithSequential(() => Task.Run(action, cancellationToken), cancellationToken);

        /// <summary>
        /// Add a continuation to be performed only after the attached task has completed.
        /// </summary>
        /// <param name="task">The previous task to be awaited on.</param>
        /// <param name="continuationFunction">The continuation to run. Generally should be an async function.</param>
        /// <param name="cancellationToken">An optional cancellation token. Will only cancel the provided action, not the sequence.</param>
        /// <returns>A task representing the provided action.</returns>
        public static Task ContinueWithSequential(this Task task, Func<Task> continuationFunction, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<bool>();

            task.ContinueWith(_ =>
            {
                // the previous task has finished execution or been cancelled, so we can run the provided continuation.

                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.SetCanceled(cancellationToken);
                }
                else
                {
                    continuationFunction().ContinueWith(continuationTask =>
                    {
                        if (cancellationToken.IsCancellationRequested || continuationTask.IsCanceled)
                        {
                            tcs.TrySetCanceled();
                        }
                        else if (continuationTask.IsFaulted)
                        {
                            tcs.TrySetException(continuationTask.Exception.AsNonNull());
                        }
                        else
                        {
                            tcs.TrySetResult(true);
                        }
                    }, cancellationToken: CancellationToken.None);
                }
            }, cancellationToken: CancellationToken.None);

            // importantly, we are not returning the continuation itself but rather a task which represents its status in sequential execution order.
            // this will not be cancelled or completed until the previous task has also.
            return tcs.Task;
        }
    }
}
