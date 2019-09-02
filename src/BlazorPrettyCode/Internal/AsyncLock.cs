using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorPrettyCode.Internal
{
    //https://stackoverflow.com/questions/21011179/how-to-protect-resources-that-may-be-used-in-a-multi-threaded-or-async-environme/21011273#21011273
    internal class AsyncLock
    {
        private readonly Task<IDisposable> _releaserTask;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly IDisposable _releaser;

        public AsyncLock()
        {
            _releaser = new Releaser(_semaphore);
            _releaserTask = Task.FromResult(_releaser);
        }

        public IDisposable Lock()
        {
            _semaphore.Wait();
            return _releaser;
        }

        public Task<IDisposable> LockAsync()
        {
            Task waitTask = _semaphore.WaitAsync();
            return waitTask.IsCompleted
                ? _releaserTask
                : waitTask.ContinueWith(
                    (_, releaser) => (IDisposable)releaser,
                    _releaser,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
        }

        private class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            public Releaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }
            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
