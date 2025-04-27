using System;
using System.Collections;
using System.Threading.Tasks;

namespace AsyncNetClient.Utils
{
    public static class TaskUtils
    {
        public static IEnumerator ToCoroutine(Func<Task> taskFactory)
        {
            return taskFactory().ToCoroutine();
        }
    }
}