﻿using System;
using System.Collections.Generic;
using AsyncNetClient.Requests;
using AsyncNetClient.Serialization;

namespace AsyncNetClient
{
    public class AsyncNetworkClientBuilder
    {
        private const int DefaultTimeout = 30;
        
        private List<IAsyncNetDecorator> _decorators;
        private TimeSpan _timeout = TimeSpan.FromSeconds(DefaultTimeout);
        private string _basePath;
        private ISerializer _serializer;
        private IRequestHandler _requestHandler;
        
        public AsyncNetworkClientBuilder WithTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
            }
            _timeout = timeout;
            return this;
        }
        
        public AsyncNetworkClientBuilder WithBasePath(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new ArgumentException("Base path cannot be null or empty.", nameof(basePath));
            }
            _basePath = basePath;
            return this;
        }
        
        public AsyncNetworkClientBuilder WithSerializer(ISerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer), "Serializer cannot be null.");
            return this;
        }
        
        public AsyncNetworkClientBuilder WithRequestHandler(IRequestHandler requestHandler)
        {
            _requestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler), "Request cannot be null.");
            return this;
        }
        
        public AsyncNetworkClientBuilder WithDecorator(IAsyncNetDecorator decorator)
        {
            if (decorator == null)
            {
                throw new ArgumentNullException(nameof(decorator), "Decorator cannot be null.");
            }
            _decorators ??= new List<IAsyncNetDecorator>();
            _decorators.Add(decorator);
            return this;
        }
        
        public IAsyncNetworkClient Build()
        {
            if (string.IsNullOrWhiteSpace(_basePath))
            {
                throw new InvalidOperationException("Base path must be set before building the client.");
            }
            if (_timeout <= TimeSpan.Zero)
            {
                throw new InvalidOperationException("Timeout must be set to a value greater than zero before building the client.");
            }
            return new AsyncNetworkClient(_requestHandler, _serializer, _basePath, _timeout,
                _decorators != null ? _decorators.ToArray() : Array.Empty<IAsyncNetDecorator>());
        }
    }
}