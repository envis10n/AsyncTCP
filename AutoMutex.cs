using System;
using System.Threading;

namespace Envis10n.AsyncTCP.Lib
{
    class AutoMutexLock<T> : IDisposable
    {
        private readonly Mutex _mutex;
        public readonly T Value;
        public AutoMutexLock(ref Mutex mutex, ref T value)
        {
            _mutex = mutex;
            Value = value;
        }
        public void Dispose()
        {
            _mutex.ReleaseMutex();
        }
    }
    class AutoMutex<T> : IDisposable
    {
        private Mutex _mutex = new Mutex();
        private T _value;
        public AutoMutex(T value)
        {
            _value = value;
        }
        public AutoMutexLock<T> Lock()
        {
            _mutex.WaitOne();
            return new AutoMutexLock<T>(ref _mutex, ref _value);
        }
        public void Dispose()
        {
            _mutex.Dispose();
        }
    }
}