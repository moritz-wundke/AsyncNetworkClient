namespace AsyncNetClient.Requests
{
    public static class RequestFactory
    {
        public static IRequest Create()
        {
            return new UnityWebRequestHandler();
        }
    }
}