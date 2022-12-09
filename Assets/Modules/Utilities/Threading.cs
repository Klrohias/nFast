using System;
using System.Threading;

namespace Klrohias.NFast.Utilities
{
    public class Threading
    {
        public static Thread RunNewThread(ThreadStart start)
        {
            var thread = new Thread(start);
            thread.Start();
            return thread;
        }

        public static void WaitUntil(Func<bool> practice, int delay = 80)
        {
            while (!practice())
            {
                Thread.Sleep(delay);
            }
        }
    }
}