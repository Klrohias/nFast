using System;
using System.Threading;
using System.Threading.Tasks;

namespace Klrohias.NFast.Utilities
{
    public class Async
    {
        public static Task RunOnThread(ThreadStart threadStart)
        {
            var result = new TaskCompletionSource<bool>();
            new Thread(() =>
            {
                threadStart();
                result.TrySetResult(true);
            }).Start();
            return result.Task;
        }

        public static Task CallbackToTask(Action<Action> action)
        {
            var result = new TaskCompletionSource<bool>();
            action(() => result.TrySetResult(true));
            return result.Task;
        }
    }
}