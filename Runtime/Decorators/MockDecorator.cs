using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Decorators
{
    public class MockDecorator : IAsyncNetDecorator
    {
        private readonly Dictionary<string, IAsyncNetDecorator.NextDecorator> _mock;
 
        /// <summary>
        /// Mock decorator ctor
        /// </summary>
        /// <param name="mock">Dictionary that maps an endpoint to a dummy response</param>
        public MockDecorator(Dictionary<string, IAsyncNetDecorator.NextDecorator> mock)
        {
            _mock = mock;
        }

        /// <inheritdoc />
        public Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator next)
        {
            if (_mock.TryGetValue(context.Path, out var mockedResponse))
            {
                return mockedResponse(context, cancellationToken);
            }
            
            return next(context, cancellationToken);
        }
    }
}