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
    }
}