using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Utils;

namespace AsyncNetClient.Decorators
{
    public class BackoffDecorator : IAsyncNetDecorator
    {
        public delegate bool IsRetryableExceptionDelegate(Exception exception);

        private readonly IDictionary<Guid, Backoff> _backoff = new ConcurrentDictionary<Guid, Backoff>();
        private readonly int _maxRetries;
        private readonly bool _jitter;
        
        private readonly double _minSeconds;
        private readonly double _maxSeconds;
        private readonly double _factor;
        
        private readonly IsRetryableExceptionDelegate _isRetryableException;
        
        public BackoffDecorator(int retries, double minSeconds, double maxSeconds, 
            double factor = Backoff.LinealFactor, bool jitter = true, IsRetryableExceptionDelegate isRetryableException = null)
        {
            _minSeconds = minSeconds;
            _maxSeconds = maxSeconds;
            _factor = factor;
            _maxRetries = retries;
            _jitter = jitter;
            _isRetryableException = isRetryableException ?? IsRetryableException;
        }
        
        private Backoff GetBackoff(RequestContext context)
        {
            if (_backoff.TryGetValue(context.Id, out var backoff))
            {
                return backoff;
            }

            var newBackoff = new Backoff(_minSeconds, _maxSeconds, _factor);
            _backoff[context.Id] = newBackoff;
            return newBackoff;
        }
        
        private void ClearBackoff(RequestContext context)
        {
            _backoff.Remove(context.Id);
        }
        
        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
#if UNITY_WEBGL
            Debug.LogError("BackoffDecorator is not supported in WebGL builds. Please use a different decorator.");
            return await next(context, cancellationToken);
#else
            var backoff = GetBackoff(context);
            try
            {
                var backoffDuration = backoff.NewAttempt(_jitter);
                if (backoffDuration > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffDuration), cancellationToken: cancellationToken);
                }
                var response = await next(context, cancellationToken);

                ClearBackoff(context);
                return response;
            }
            catch (Exception ex)
            {
                if (backoff.Attempts > _maxRetries)
                {
                    ClearBackoff(context);
                    throw new MaxRetriesExceededException();
                }

                if (!_isRetryableException(ex))
                {
                    ClearBackoff(context);
                    throw;
                }
                   
                context.Reset(this);
                return await SendAsync(context, cancellationToken, next);
            }
#endif
        }
        
        private static bool IsRetryableException(Exception exception)
        {
            return exception is TimeoutException;
        }
    }
}