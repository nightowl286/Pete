using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pete.Other
{
    public class ThreadedQueue
    {
        #region Subclass
        private class QueueItem : IDisposable
        {
            #region Fields
            private Func<CancellationToken, Task> _Action;
            private bool _Disposed = false;
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
                    Debug.WriteLine($"QueueItemCancelled");
                    _Cancellation.Cancel();
                    Cancelled = true;
                }
            }
            #endregion

            #region IDisposable
            public void Dispose()
            {
                if (_Disposed) return;

                _Task?.Dispose();
                _Cancellation?.Dispose();
                GC.SuppressFinalize(this);

                _Disposed = true;
            }
            #endregion
        }
        #endregion

        #region Private
        private List<QueueItem> _Scheduled;
        private Mutex _Mutex;
        private Task _WorkTask;
        #endregion
        public ThreadedQueue()
        {
            _Mutex = new Mutex();
            _Scheduled = new List<QueueItem>();
            Log("ctor");
        }

        #region Methods
        public bool CancelAllThenSchedule(Func<CancellationToken, Task> action, int msTimeout = Timeout.Infinite)
        {
            if (_Mutex.WaitOne(msTimeout))
            {
                CancelAllCore();
                ScheduleCore(action);
                _Mutex.ReleaseMutex();
                return true;
            }
            return false;
        }
        public bool Schedule(Func<CancellationToken, Task> action, int msTimeout = Timeout.Infinite)
        {
            if (_Mutex.WaitOne(msTimeout))
            {
                ScheduleCore(action);
                _Mutex.ReleaseMutex();
                return true;
            }
            return false;
        }
        public bool CancelAll(int msTimeout = Timeout.Infinite)
        {
            if (_Mutex.WaitOne(msTimeout))
            {
                CancelAllCore();
                _Mutex.ReleaseMutex();
                return true;
            }
            return false;
        }
        private async Task TaskAction()
        {
            Debug.WriteLine("");
            Log("TaskAction entry");
            while (true)
            {
                await SlowMutexWait();
                RemoveCancelled();

                if (_Scheduled.TryPeek(out QueueItem item))
                {
                    _Mutex.ReleaseMutex();
                    try
                    {
                        Task task = item.PrepareTask();
                        Log($"awaiting scheduled task | token {item._Cancellation.GetHashCode():x2}");
                        await task;
                        Log($"scheduled task finished ({task.Status})");
                    }
                    catch (Exception ex)
                    {
                        Log($"scheduled task cancelled {ex.Message}");
                    }

                    await SlowMutexWait();
                    _ = _Scheduled.Dequeue();

                    RemoveCancelled();

                    _Mutex.ReleaseMutex();
                }
                else
                {
                    _Mutex.ReleaseMutex();
                    Log("TaskAction leave");
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
        private async Task SlowMutexWait(int msTimeout = 10, int msDelay = 10)
        {
            while (true)
            {
                if (_Mutex.WaitOne(msTimeout))
                    return;
                await Task.Delay(msDelay);
            }
        }
        private void ScheduleCore(Func<CancellationToken, Task> action)
        {
            _Scheduled.Add(new QueueItem(action));
            Log($"scheduled new action. count: {_Scheduled.Count:n0}");

            if (_WorkTask?.IsCompleted != false)
            {
                Log("starting work task");
                _WorkTask = TaskAction();
                _WorkTask.ConfigureAwait(false);

            }
        }
        private void CancelAllCore()
        {
            Log($"Cancelling {_Scheduled.Count} task/s");
            foreach(QueueItem item in _Scheduled)
                item.Cancel();
        }
        #endregion
    }
}
