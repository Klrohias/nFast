using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Klrohias.NFast.Utilities
{
    public class ThreadDispatcher
    {
        public event Action<Exception> OnException;

        public void Dispatch(Action action)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }

        public Task DispatchAndWait(Action action)
        {
            var t = new TaskCompletionSource<bool>();
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() =>
                {
                    action();
                    t.TrySetResult(true);
                });
            }
            return t.Task;
        }
        public void Stop() => _running = false;

        public void Start()
        {
            if (!_running)
            {
                Threading.RunNewThread(DispatcherThread);
            }
            _running = true;
        }

        private bool _running = false;
        private readonly Queue<Action> _actionQueue = new Queue<Action>();
        private void DispatcherThread()
        {
            while (_running)
            {
                Thread.Sleep(0);
                lock (_actionQueue)
                {
                    while (_actionQueue.Count > 0)
                    {
                        try
                        {
                            _actionQueue.Dequeue()();
                        }
                        catch (Exception e)
                        {
                            OnException?.Invoke(e);
                        }
                    }
                }
            }
        }
    }
}