using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AsyncNetClient.Utils
{
    public static class AsyncExtensions
    {
        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch(OperationCanceledException)
            {
                // Ignore cancellation exceptions
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static async void Then(this Task task, Action then, Action<Exception> error = null, Action final = null)
        {
            try
            {
                await task;
                then();
            }
            catch(Exception e)
            {
                error?.Invoke(e);
            }
            finally
            {
                final?.Invoke();
            }
        }
        
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            TaskCompletionSource<AsyncOperation> taskCompletionSource = new();
            asyncOp.completed += taskCompletionSource.SetResult;
            return ((Task) taskCompletionSource.Task).GetAwaiter();
        }

        public static Task WithCancellation(this AsyncOperation asyncOp, CancellationToken cancellationToken, bool cancelImmediately = false)
        {
            TaskCompletionSource<AsyncOperation> taskCompletionSource = new();

            if(cancelImmediately && cancellationToken.CanBeCanceled)
            {
                _ = cancellationToken.Register((() =>
                {
                    taskCompletionSource.TrySetCanceled(cancellationToken);
                }));
            }

            asyncOp.completed += operation =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    if(!cancelImmediately)
                    {
                        taskCompletionSource.TrySetCanceled(cancellationToken);
                    }
                    return;
                }
                taskCompletionSource.SetResult(operation);
            };
            return taskCompletionSource.Task;
        }
        
        public static IEnumerator ToCoroutine<T>(this Task<T> task, Action<T> resultHandler = null, Action<Exception> exceptionHandler = null)
        {
            return new ToCoroutineEnumerator<T>(task, resultHandler, exceptionHandler);
        }

        public static IEnumerator ToCoroutine(this Task task, Action<Exception> exceptionHandler = null)
        {
            return new ToCoroutineEnumerator(task, exceptionHandler);
        }
        
        sealed class ToCoroutineEnumerator : IEnumerator
        {
            bool completed;
            Task task;
            Action<Exception> exceptionHandler = null;
            bool isStarted = false;
            ExceptionDispatchInfo exception;

            public ToCoroutineEnumerator(Task task, Action<Exception> exceptionHandler)
            {
                completed = false;
                this.exceptionHandler = exceptionHandler;
                this.task = task;
            }

            async Task RunTask(Task task)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    else
                    {
                        this.exception = ExceptionDispatchInfo.Capture(ex);
                    }
                }
                finally
                {
                    completed = true;
                }
            }

            public object Current => null;

            public bool MoveNext()
            {
                if (!isStarted)
                {
                    isStarted = true;
                    RunTask(task).Forget();
                }

                if (exception != null)
                {
                    exception.Throw();
                    return false;
                }

                return !completed;
            }

            void IEnumerator.Reset()
            {
            }
        }

        sealed class ToCoroutineEnumerator<T> : IEnumerator
        {
            bool completed;
            Action<T> resultHandler = null;
            Action<Exception> exceptionHandler = null;
            bool isStarted = false;
            Task<T> task;
            object current = null;
            ExceptionDispatchInfo exception;

            public ToCoroutineEnumerator(Task<T> task, Action<T> resultHandler, Action<Exception> exceptionHandler)
            {
                completed = false;
                this.task = task;
                this.resultHandler = resultHandler;
                this.exceptionHandler = exceptionHandler;
            }

            async Task RunTask(Task<T> task)
            {
                try
                {
                    var value = await task;
                    current = value; // boxed if T is struct...
                    if (resultHandler != null)
                    {
                        resultHandler(value);
                    }
                }
                catch (Exception ex)
                {
                    if (exceptionHandler != null)
                    {
                        exceptionHandler(ex);
                    }
                    else
                    {
                        this.exception = ExceptionDispatchInfo.Capture(ex);
                    }
                }
                finally
                {
                    completed = true;
                }
            }

            public object Current => current;

            public bool MoveNext()
            {
                if (!isStarted)
                {
                    isStarted = true;
                    RunTask(task).Forget();
                }

                if (exception != null)
                {
                    exception.Throw();
                    return false;
                }

                return !completed;
            }

            void IEnumerator.Reset()
            {
            }
        }
    }
}