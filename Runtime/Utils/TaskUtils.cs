using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncNetClient.Utils
{
    public static class TaskUtils
    {
        public static IEnumerator ToCoroutine(Func<Task> taskFactory)
        {
            return taskFactory().ToCoroutine();
        }
        
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DelayAsync(int milliseconds, System.Action callback);
#endif
        
        public static async Task Delay(double seconds, CancellationToken cancellationToken)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var tcs = new TaskCompletionSource<bool>();
            var milliseconds = (int)(seconds * 1000);

            DelayAsync(milliseconds, () => tcs.SetResult(true));

            await tcs.Task;
#else
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
#endif
        }
    }
}