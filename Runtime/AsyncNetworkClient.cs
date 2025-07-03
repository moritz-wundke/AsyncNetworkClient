using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Requests;
using AsyncNetClient.Serialization;

namespace AsyncNetClient
{
    internal class AsyncNetworkClient : IAsyncNetworkClient
    {
        private IAsyncNetDecorator[] _decorators;
        private readonly TimeSpan _timeout;
        private readonly string _basePath;

        private ISerializer Serializer { get; }
        
        private IRequestHandler RequestHandler { get; }

        private class AsyncNetworkClientDecorator : IAsyncNetDecorator
        {
            private readonly AsyncNetworkClient _client;
            public AsyncNetworkClientDecorator(AsyncNetworkClient client)
            {
                _client = client;
            }
            
            public Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken, IAsyncNetDecorator.NextDecorator _)
            {
                return _client.SendAsync(context, cancellationToken);
            }
        }

        public AsyncNetworkClient(IRequestHandler requestHandler, ISerializer serializer, string basePath, TimeSpan timeout,
            params IAsyncNetDecorator[] decorators)
        {
            RequestHandler = requestHandler ?? RequestFactory.Create();
            Serializer = serializer ?? SerializationFactory.Create();
            _basePath = basePath;
            _timeout = timeout;
            _decorators = new IAsyncNetDecorator[decorators.Length + 1];
            Array.Copy(decorators, _decorators, decorators.Length);
            _decorators[^1] = new AsyncNetworkClientDecorator(this);
        }
        
        public AsyncNetworkClient(string basePath, TimeSpan timeout, params IAsyncNetDecorator[] decorators)
            : this(RequestFactory.Create(), SerializationFactory.Create(), basePath, timeout, decorators)
        {
        }
        
        public async Task<ResponseContext> SendAsync(HttpMethod method, string path, object request = null, CancellationToken cancellationToken = default)
        {
            var requestContext = new RequestContext(Serializer, method, _basePath, path, request, _timeout, _decorators);
            var response = await InvokeRecursive(requestContext, cancellationToken);
            return response;
        }
        
        private static Task<ResponseContext> InvokeRecursive(RequestContext context, CancellationToken cancellationToken)
        {
            return context.GetNextDecorator().SendAsync(context, cancellationToken, InvokeRecursive);
        }
     
        private async Task<ResponseContext> SendAsync(
            RequestContext context, CancellationToken cancellationToken)
        {
            return await RequestHandler.SendAsync(context, cancellationToken);
        }
    }
}