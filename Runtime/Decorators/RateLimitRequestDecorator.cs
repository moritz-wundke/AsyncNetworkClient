using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Utils;

namespace AsyncNetClient.Decorators
{
    public class RateLimitRequestDecorator : IAsyncNetDecorator
    {
        private readonly ConcurrentQueue<(TaskCompletionSource<ResponseContext>, RequestContext, CancellationToken, IAsyncNetDecorator.NextDecorator)> _queue = new();
        private readonly int _maxConcurrentRequests;
        private volatile int _requestCount;
        
        public RateLimitRequestDecorator(int maxConcurrentRequests)
        {
            _maxConcurrentRequests = maxConcurrentRequests;
        }
 
        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
            if (_requestCount >= _maxConcurrentRequests)
            {
                var completionSource = new TaskCompletionSource<ResponseContext>();
                _queue.Enqueue((completionSource, context, cancellationToken, next));
                return await completionSource.Task;
            }
            return await RunNext(context, cancellationToken, next);
        }

        private async Task<ResponseContext> RunNext(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
            Interlocked.Increment(ref _requestCount);
            try
            {
                return await next(context, cancellationToken);
            }
            finally
            {
                TryDequeueAndRunNext().Forget();
                Interlocked.Decrement(ref _requestCount);
            }
        }

        private async Task TryDequeueAndRunNext()
        {
            if (_queue.TryDequeue(out var item))
            {
                Interlocked.Increment(ref _requestCount);
                
                try
                {
                    var response = await item.Item4(item.Item2, item.Item3);
                    item.Item1.TrySetResult(response);
                }
                catch (Exception ex)
                {
                    item.Item1.TrySetException(ex);
                }
                finally
                {
                    TryDequeueAndRunNext().Forget();
                    Interlocked.Decrement(ref _requestCount);
                }
            }
        }
    }
}