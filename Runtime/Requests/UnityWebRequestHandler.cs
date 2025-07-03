using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncNetClient.Utils;
using UnityEngine.Networking;

namespace AsyncNetClient.Requests
{
    public class UnityWebRequestHandler : IRequest
    {
        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken)
        {
            using var req = CreateRequest(context);
     
            // Add the timeout via a cancellation token
            var linkToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkToken.CancelAfter(context.Timeout);
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
            return new ResponseContext(context,
                req.downloadHandler.data,
                req.result,
                (int)req.responseCode,
                req.error,
                req.GetResponseHeaders());
        }
        
        private UnityWebRequest CreateRequest(RequestContext context)
        {
            var url = context.BasePath + context.Path;
            UnityWebRequest req;

            switch (context.Method)
            {
                case HttpMethod.Post:
                {
                    var data = context.Serializer.SerializeObject(context.Value);
                    var bodyRequest = new System.Text.UTF8Encoding().GetBytes(data);
                    req = new UnityWebRequest(url, HttpMethod.Post.ToString())
                    {
                        uploadHandler = new UploadHandlerRaw(bodyRequest)
                        {
                            contentType = context.Serializer.ContentType
                        },
                        downloadHandler = new DownloadHandlerBuffer()
                    };
                    break;
                }
                case HttpMethod.Get:
                {
                    if (context.Value is (string, string)[] args)
                    {
                        foreach (var arg in args)
                        {
                            url += $"{arg.Item1}={arg.Item2}&";
                        }
                    }

                    req = UnityWebRequest.Get(url);
                    break;
                }
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