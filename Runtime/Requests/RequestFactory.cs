namespace AsyncNetClient.Requests
{
    public static class RequestFactory
    {
        public static IRequestHandler Create()
        {
#if UNITY_WEBGL
            return new UnityWebRequestHandlerHandler();
#else
            return new HttpClientRequestHandler();
#endif
        }
    }
}