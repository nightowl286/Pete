using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pete.Other
{
    public class ThreadedQueue : IDisposable
    {
        #region Subclass
        private class QueueItem : IDisposable
        {
            #region Fields
            private readonly Func<CancellationToken, Task> _Action;
            public CancellationTokenSource _Cancellation;
            public Task _Task;
            public CancellationToken Token => _Cancellation.Token;
            #endregion

            #region Properties
            public bool Cancelled { get; private set; }
            #endregion
            public QueueItem(Func<CancellationToken, Task> action)
            {
                _Action = action;
            }

            #region Methods
            public Task PrepareTask()
            {
                _Cancellation = new CancellationTokenSource();
                _Task = _Action(_Cancellation.Token);
                _Task.ConfigureAwait(false);
                return _Task;
            }
            public void Cancel()
            {
                if (_Task?.IsCompleted == false && _Cancellation?.IsCancellationRequested == false)
                {
                   //Debug.WriteLine($"QueueItemCancelled");
                    _Cancellation.Cancel();
                    Cancelled = true;
                }
            }
            #endregion

            #region IDisposable
            public void Dispose()
            {
                _Task?.Dispose();
                _Cancellation?.Dispose();
            }
            #endregion
        }
        private struct OwnershipToken : IDisposable
        {
            private readonly ThreadedQueue _Queue;
            public OwnershipToken(ThreadedQueue queue)
            {
                _Queue = queue;
                queue.GetOwnership();
            }

            public void Dispose() => _Queue.ReleaseOwnership(); 
        }
        #endregion

        #region Private
        private readonly List<QueueItem> _Scheduled;
        private const int HAS_OWNERSHIP = 1;
        private const int NO_OWNERSHIP = 0;
        private int _Ownership;
        private Task _WorkTask;
        #endregion
        public ThreadedQueue()
        {
            _Scheduled = new List<QueueItem>();
            //Log("ctor");
        }

        #region Methods
        private OwnershipToken Claim() => new OwnershipToken(this);
        public void CancelAllThenSchedule(Func<CancellationToken, Task> action)
        {
            using (Claim())
            {
                CancelAllCore();
                ScheduleCore(action);
            }
        }
        public void Schedule(Func<CancellationToken, Task> action)
        {
            using (Claim())
                ScheduleCore(action);
        }
        public void CancelAll()
        {
            using (Claim())
                CancelAllCore();
        }
        private bool GetOwnership() => Interlocked.CompareExchange(ref _Ownership, HAS_OWNERSHIP, NO_OWNERSHIP) == NO_OWNERSHIP;
        private void ReleaseOwnership() => Interlocked.Exchange(ref _Ownership, NO_OWNERSHIP);
        private async Task TaskAction()
        {
            Debug.WriteLine("");
            //Log("TaskAction entry");
            while (true)
            {
                await SlowMutexWait();
                RemoveCancelled();

                if (TryPeek(out QueueItem item))
                {
                    ReleaseOwnership();
                    try
                    {
                        Task task = item.PrepareTask();
                        //Log($"awaiting scheduled task | token {item._Cancellation.GetHashCode():x2}");
                        await task;
                        //Log($"scheduled task finished ({task.Status})");
                    }
                    catch (Exception ex)
                    {
                        //Log($"scheduled task cancelled {ex.Message}");
                    }

                    await SlowMutexWait();
                    _ = Dequeue();

                    RemoveCancelled();

                    ReleaseOwnership();
                }
                else
                {
                    ReleaseOwnership();
                    //Log("TaskAction leave");
                    return;
                }

            }
        }
        [Conditional("DEBUG")]
        private void Log(string text) => Debug.WriteLine($"[ThreadedQueue:{GetHashCode():x2}] {text}");
        private void RemoveCancelled()
        {
            for (int i = _Scheduled.Count - 1; i >= 0; i--)
            {
                if (_Scheduled[i].Cancelled)
                    _Scheduled.RemoveAt(i);
            }
        }
        private async Task SlowMutexWait(int msDelay = 10)
        {
            while (true)
            {
                if (GetOwnership())
                    return;
                await Task.Delay(msDelay);
            }
        }
        private void ScheduleCore(Func<CancellationToken, Task> action)
        {
            _Scheduled.Add(new QueueItem(action));
            //Log($"scheduled new action. count: {_Scheduled.Count:n0}");

            if (_WorkTask?.IsCompleted != false)
            {
                //Log("starting work task");
                _WorkTask = TaskAction();
                _WorkTask.ConfigureAwait(false);

            }
        }
        private void CancelAllCore()
        {
            //Log($"Cancelling {_Scheduled.Count} task/s");
            foreach(QueueItem item in _Scheduled)
                item.Cancel();
        }
        public void Dispose()
        {
            _WorkTask?.Dispose();
            foreach (QueueItem item in _Scheduled)
                item.Dispose();
        }
        private bool TryPeek(out QueueItem item)
        {
            if (_Scheduled.Count > 0)
            {
                item = _Scheduled[0];
                return true;
            }
            item = default;
            return false;
        }
        private QueueItem Dequeue()
        {
            if (_Scheduled.Count == 0) throw new InvalidOperationException("The collection is empty");

            QueueItem item = _Scheduled[0];
            _Scheduled.RemoveAt(0);
            return item;
        }
        #endregion
    }
}
