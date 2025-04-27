using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Decorators
{
    public class HeadersDecorator : IAsyncNetDecorator
    {
        private readonly Dictionary<string, string> _requestHeaders;
        
        public HeadersDecorator(Dictionary<string, string> requestHeaders)
        {
            _requestHeaders = requestHeaders;
        }
        
        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
            if (_requestHeaders == null || _requestHeaders.Count == 0)
            {
                // No headers to add, just call the next decorator
                return await next(context, cancellationToken);
            }
            
            foreach (var keyValuePair in _requestHeaders)
            {
                if (string.IsNullOrWhiteSpace(keyValuePair.Value))
                {
                    continue;
                }

                context.RequestHeaders[keyValuePair.Key] = keyValuePair.Value;
            }
            
            return await next(context, cancellationToken);
        }
    }
}