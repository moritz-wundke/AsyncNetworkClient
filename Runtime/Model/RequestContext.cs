using System;
using System.Collections.Generic;
using AsyncNetClient.Serialization;

namespace AsyncNetClient
{
    public class RequestContext
    {
        int decoratorIndex;
        readonly IAsyncNetDecorator[] decorators;
        Dictionary<string, string> headers;
        
        public Guid Id { get; }
        
        public ISerializer Serializer { get; }
        
        public HttpMethod Method { get; set; }
 
        public string BasePath { get; }
        public string Path { get; }
        public object Value { get; }
        public TimeSpan Timeout { get; }
        public DateTimeOffset Timestamp { get; private set; }
 
        public IDictionary<string, string> RequestHeaders => headers ??= new Dictionary<string, string>();

        public RequestContext(ISerializer serializer, HttpMethod method, string basePath, string path, object value, TimeSpan timeout, IAsyncNetDecorator[] filters)
        {
            Id = Guid.NewGuid();
            Serializer = serializer;
            Method = method;
            decoratorIndex = -1;
            decorators = filters;
            BasePath = basePath;
            Path = path;
            Value = value;
            Timeout = timeout;
            Timestamp = DateTimeOffset.UtcNow;
        }
 
        internal Dictionary<string, string> GetRawHeaders() => headers;
        internal IAsyncNetDecorator GetNextDecorator() => decorators[++decoratorIndex];
 
        public void Reset(IAsyncNetDecorator currentFilter)
        {
            decoratorIndex = Array.IndexOf(decorators, currentFilter);
            headers?.Clear();
            Timestamp = DateTimeOffset.UtcNow;
        }
    }
}