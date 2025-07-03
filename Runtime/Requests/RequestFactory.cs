namespace AsyncNetClient.Requests
{
    public static class RequestFactory
    {
        public static IRequestHandler Create()
        {
            return new UnityWebRequestHandlerHandler();
        }
    }
}