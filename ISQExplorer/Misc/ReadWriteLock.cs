using System;
using System.Threading;
using System.Threading.Tasks;

namespace ISQExplorer.Misc
{
    public class ReadWriteLock
    {
        private readonly ReaderWriterLock _lock;

        public ReadWriteLock()
        {
            _lock = new ReaderWriterLock();
        }

        public T Read<T>(Func<T> func)
        {
            _lock.AcquireReaderLock(-1);
            try
            {
                return func();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public void Read(Action func)
        {
            _lock.AcquireReaderLock(-1);
            try
            {
                func();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public async Task<T> ReadAsync<T>(Func<Task<T>> func)
        {
            _lock.AcquireReaderLock(-1);
            try
            {
                return await func();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public async Task ReadAsync(Func<Task> func)
        {
            _lock.AcquireReaderLock(-1);
            try
            {
                await func();
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }
        }

        public T Write<T>(Func<T> func)
        {
            _lock.AcquireWriterLock(-1);
            try
            {
                return func();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public void Write(Action func)
        {
            _lock.AcquireWriterLock(-1);
            try
            {
                func();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public async Task<T> WriteAsync<T>(Func<Task<T>> func)
        {
            _lock.AcquireWriterLock(-1);
            try
            {
                return await func();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        public async Task WriteAsync(Func<Task> func)
        {
            _lock.AcquireWriterLock(-1);
            try
            {
                await func();
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }
    }
}