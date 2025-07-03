using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Decorators
{
    public class LoggingDecorator : IAsyncNetDecorator
    {
        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                UnityEngine.Debug.Log($"Start Network Request: {context.Path}, timestamp: {context.Timestamp}, method: {context.Method}");
 
                var response = await next(context, cancellationToken);
                
                if (response.IsError)
                {
                    UnityEngine.Debug.LogError($"Request Error: {context.Path}, Elapsed: {sw.Elapsed}, Code: {response.StatusCode} Message: {response.Error}");
                }
                else
                {
                    UnityEngine.Debug.Log($"Complete Network Request: {context.Path} , Elapsed: {sw.Elapsed}, Size: {response.GetRawData().Length}");
                }
 
                return response;
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case OperationCanceledException:
                        UnityEngine.Debug.Log($"Request Canceled: {context.Path}");
                        break;
                    case TimeoutException:
                        UnityEngine.Debug.LogError($"Request Timeout: {context.Path}");
                        break;
                }

                throw;
            }
        }
    }
}