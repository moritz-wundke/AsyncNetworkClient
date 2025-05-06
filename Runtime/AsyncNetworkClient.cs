using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Serialization;
using AsyncNetClient.Utils;
#if WITH_NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif
using UnityEngine.Networking;

namespace AsyncNetClient
{
    public class AsyncNetworkClient : IAsyncNetworkClient
    {
        private IAsyncNetDecorator[] _decorators;
        private readonly TimeSpan _timeout;
        private readonly string _basePath;

        public ISerializer Serializer { get; }

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
        
#if WITH_NEWTONSOFT_JSON
        public AsyncNetworkClient(JsonSerializerSettings settings, string basePath, TimeSpan timeout,
            params IAsyncNetDecorator[] decorators)
        {
            // We could use other formats like Protobuf, but for now we use JSON
            Serializer = SerializationFactory.Create(settings);
            _basePath = basePath;
            _timeout = timeout;
            _decorators = new IAsyncNetDecorator[decorators.Length + 1];
            Array.Copy(decorators, _decorators, decorators.Length);
            _decorators[^1] = new AsyncNetworkClientDecorator(this);
        }
#endif
        
        public AsyncNetworkClient(string basePath, TimeSpan timeout, params IAsyncNetDecorator[] decorators)
        {
            // We could use other formats like Protobuf, but for now we use JSON
            Serializer = SerializationFactory.Create();
            _basePath = basePath;
            _timeout = timeout;
            _decorators = new IAsyncNetDecorator[decorators.Length + 1];
            Array.Copy(decorators, _decorators, decorators.Length);
            _decorators[^1] = new AsyncNetworkClientDecorator(this);
        }
        
        public void AddDecorator(IAsyncNetDecorator decorator)
        {
            Array.Resize(ref _decorators, _decorators.Length + 1);
            Array.Copy(_decorators, _decorators.Length - 2, _decorators, _decorators.Length - 1, 1);
            _decorators[^2] = decorator;
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
            // TODO: Architecture - Add request abstraction o be able to use other
            // libraries and http clients (consoles, etc)
            using var req = CreateRequest(context);
     
            // Add the timeout via a cancellation token
            var linkToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkToken.CancelAfter(_timeout);
            try
            {
                await req.SendWebRequest().WithCancellation(cancellationToken:linkToken.Token);
            }
            catch (OperationCanceledException)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException();
                }
            }
            finally
            {
                // stop CancelAfter
                if (!linkToken.IsCancellationRequested)
                {
                    linkToken.Cancel();
                }
            }
            
            // TODO: Optimization - Only access dara when he response context requires it,
            // the response context should be a disposable too in this case
            return new ResponseContext(context, req);
        }
        
        private UnityWebRequest CreateRequest(RequestContext context)
        {
            var url = _basePath + context.Path;
            UnityWebRequest req;

            switch (context.Method)
            {
                case HttpMethod.Post:
                {
                    var data = Serializer.SerializeObject(context.Value);
                    var bodyRequest = new System.Text.UTF8Encoding().GetBytes(data);
                    req = new UnityWebRequest(url, HttpMethod.Post.ToString())
                    {
                        uploadHandler = new UploadHandlerRaw(bodyRequest)
                        {
                            contentType = Serializer.ContentType
                        },
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                    break;
                }
                case HttpMethod.Get:
                    req = UnityWebRequest.Get(url);
                    break;
                default:
                    throw new NotSupportedException($"HTTP method '{context.Method}' is not supported.");
            }

            var header = context.GetRawHeaders();
            if (header == null)
            {
                return req;
            }
            foreach (var item in header)
            {
                req.SetRequestHeader(item.Key, item.Value);
            }

            return req;
        }
    }
}