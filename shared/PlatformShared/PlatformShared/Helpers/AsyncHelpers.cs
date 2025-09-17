using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformShared.Helpers
{
    // This is the best way to run an asynchronous method in a synchronous method.  
    // It creates a separete synchronization context on which to run the Task.
    // This avoids deadlocks because the running task may yield execution back to the calling context, which is currently waiting for the completion of this task.
    // If this occurs, the calling context is currently blocked because it's waiting on this Task to finish and it will deadlock because it can never yield execution back again.
    // This class doesn't completely eliminate the chance of deadlocks because it still depends on the ThreadPool to allocate a new Thread.
    // If there aren't any available and won't be until the calling context frees its Thread, then a deadlock could occur.
    // However, the ThreadPool is managed by .NET and allocated based on system resources so this represents a significantly less likely scenario than the scenario it solves for.
    // It's the best way to run an async method in a sync method.

    public static class AsyncHelpers
    {
        public static void RunSync(Func<Task> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            synch.Post(async _ =>
            {
                try
                {
                    await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();

            SynchronizationContext.SetSynchronizationContext(oldContext);
        }

        public static T RunSync<T>(Func<Task<T>> task)
        {
            var oldContext = SynchronizationContext.Current;
            var synch = new ExclusiveSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(synch);
            var ret = default(T);
            synch.Post(async _ =>
            {
                try
                {
                    ret = await task();
                }
                catch (Exception e)
                {
                    synch.InnerException = e;
                    throw;
                }
                finally
                {
                    synch.EndMessageLoop();
                }
            }, null);
            synch.BeginMessageLoop();
            SynchronizationContext.SetSynchronizationContext(oldContext);
            return ret;
        }

        private class ExclusiveSynchronizationContext : SynchronizationContext
        {
            private readonly Queue<Tuple<SendOrPostCallback, object>> _items =
                new Queue<Tuple<SendOrPostCallback, object>>();

            private readonly AutoResetEvent _workItemsWaiting = new AutoResetEvent(false);

            private bool done;
            public Exception InnerException { get; set; }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotSupportedException("We cannot send to our same thread");
            }

            public override void Post(SendOrPostCallback d, object state)
            {
                lock (_items)
                {
                    _items.Enqueue(Tuple.Create(d, state));
                }

                _workItemsWaiting.Set();
            }

            public void EndMessageLoop()
            {
                Post(_ => done = true, null);
            }

            public void BeginMessageLoop()
            {
                while (!done)
                {
                    Tuple<SendOrPostCallback, object> task = null;
                    lock (_items)
                    {
                        if (_items.Count > 0)
                        {
                            task = _items.Dequeue();
                        }
                    }

                    if (task != null)
                    {
                        task.Item1(task.Item2);
                        if (InnerException != null) // the method threw an exeption
                        {
                            throw new AggregateException("AsyncHelpers.Run method threw an exception.", InnerException);
                        }
                    }
                    else
                    {
                        _workItemsWaiting.WaitOne();
                    }
                }
            }

            public override SynchronizationContext CreateCopy()
            {
                return this;
            }
        }
    }
}
