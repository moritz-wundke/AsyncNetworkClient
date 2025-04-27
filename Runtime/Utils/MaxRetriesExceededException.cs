using System;

namespace AsyncNetClient.Utils
{
    public class MaxRetriesExceededException : Exception
    {
        public MaxRetriesExceededException() : base("Maximum number of retries exceeded.") { }
    }
}