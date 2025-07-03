using System;
using System.Collections.Generic;
using System.Text;
using AsyncNetClient.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace AsyncNetClient
{
    public class ResponseContext
    {
        private readonly byte[] _bytes;
        public long StatusCode { get; }
        public Dictionary<string, string> ResponseHeaders { get; }
        
        public bool IsError { get; }
        
        public bool IsSuccess { get; }
        
        public string Error { get; }
        
        public UnityWebRequest.Result ResultState { get; }
        
        public DateTimeOffset Timestamp { get; }
        
        public RequestContext RequestContext { get; }
        

        public ResponseContext(RequestContext requestContext, byte[] bytes, UnityWebRequest.Result resultState, long statusCode, string error, Dictionary<string, string> responseHeaders)
        {
            RequestContext = requestContext;
            _bytes = bytes;
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            ResultState = resultState;
            IsSuccess = resultState == UnityWebRequest.Result.Success;
            IsError = !IsSuccess;
            Error = $"{statusCode}: {error}";
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        public ResponseContext(RequestContext requestContext, byte[] bytes, long statusCode, Dictionary<string, string> responseHeaders)
        {
            RequestContext = requestContext;
            _bytes = bytes;
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            IsSuccess = statusCode is >= 200 and < 300;
            ResultState = IsSuccess ? UnityWebRequest.Result.Success : UnityWebRequest.Result.ProtocolError;
            IsError = !IsSuccess;
            Error = $"Error: {statusCode}";
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        public ResponseContext(RequestContext requestContext, byte[] bytes, long statusCode, string error, Dictionary<string, string> responseHeaders)
        {
            RequestContext = requestContext;
            _bytes = bytes;
            StatusCode = statusCode;
            ResponseHeaders = responseHeaders;
            IsSuccess = statusCode is >= 200 and < 300;
            ResultState = IsSuccess ? UnityWebRequest.Result.Success : UnityWebRequest.Result.ProtocolError;
            IsError = !IsSuccess;
            Error = $"{statusCode}: {error}";
            Timestamp = DateTimeOffset.UtcNow;
        }
        
        public ResponseContext(RequestContext requestContext, object body, long statusCode, Dictionary<string, string> responseHeaders)
            : this(requestContext, Encoding.UTF8.GetBytes(requestContext.Serializer.SerializeObject(body)), statusCode, responseHeaders)
        {
        }
 
        public byte[] GetRawData() => _bytes;
 
        public T GetResponseAs<T>()
        {
            return
                RequestContext.Serializer.DeserializeObject<T>(
                    Encoding.UTF8.GetString(_bytes));
        }
    }
}