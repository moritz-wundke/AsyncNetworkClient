using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Requests
{
    public class HttpClientRequestHandler : IRequestHandler, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _disposeHttpClient;
        private static readonly Dictionary<string, string> EmptyHeaders = new Dictionary<string, string>();

        public HttpClientRequestHandler() : this(new HttpClient(), true)
        {
        }

        public HttpClientRequestHandler(HttpClient httpClient, bool disposeHttpClient = false)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _disposeHttpClient = disposeHttpClient;
        }

        public async Task<ResponseContext> SendAsync(RequestContext context, CancellationToken cancellationToken)
        {
            using var request = CreateHttpRequestMessage(context);

            // Add timeout via cancellation token
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(context.Timeout);

            try
            {
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);

                // Use ConfigureAwait(false) to avoid context switching
                var responseData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                var responseHeaders = ExtractHeaders(response);

                var errorMessage = response.IsSuccessStatusCode ? null :
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

                return new ResponseContext(context, responseData, (int)response.StatusCode, errorMessage, responseHeaders);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Request to {context.BasePath}{context.Path} timed out after {context.Timeout}");
            }
        }

        private static Dictionary<string, string> ExtractHeaders(HttpResponseMessage response)
        {
            var responseHeaders = new Dictionary<string, string>();

            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value);
            }

            return responseHeaders.Count > 0 ? responseHeaders : EmptyHeaders;
        }

        private HttpRequestMessage CreateHttpRequestMessage(RequestContext context)
        {
            var url = BuildUrl(context);
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url)
            };

            switch (context.Method)
            {
                case HttpMethod.Get:
                    request.Method = System.Net.Http.HttpMethod.Get;
                    break;

                case HttpMethod.Post:
                    request.Method = System.Net.Http.HttpMethod.Post;
                    if (context.Value != null)
                    {
                        var data = context.Serializer.SerializeObject(context.Value);
                        request.Content = new StringContent(data, Encoding.UTF8, context.Serializer.ContentType);
                    }
                    break;

                default:
                    throw new NotSupportedException($"HTTP method '{context.Method}' is not supported.");
            }

            AddCustomHeaders(request, context);
            return request;
        }

        private static string BuildUrl(RequestContext context)
        {
            var url = context.BasePath + context.Path;

            if (context.Method == HttpMethod.Get && context.Value is (string, string)[] args)
            {
                var queryString = new StringBuilder();
                foreach (var arg in args)
                {
                    if (queryString.Length > 0)
                    {
                        queryString.Append("&");
                    }
                    queryString.Append($"{Uri.EscapeDataString(arg.Item1)}={Uri.EscapeDataString(arg.Item2)}");
                }
                if (queryString.Length > 0)
                {
                    url += $"{(url.Contains("?") ? "&" : "?")}{queryString}";
                }
            }

            return url;
        }

        private static void AddCustomHeaders(HttpRequestMessage request, RequestContext context)
        {
            var headers = context.GetRawHeaders();
            if (headers == null) return;

            foreach (var header in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        public void Dispose()
        {
            if (_disposeHttpClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}