using System;
using System.Buffers;
using Db.Storage;

namespace Db.Api.Storage
{
    public sealed class DataStore : IDisposable
    {
        private readonly Store _store;
        private readonly MemoryPool<byte> _pool;

        public DataStore(MemoryPool<byte> pool, Store store)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _store = store ?? throw new ArgumentNullException(nameof(Store));
        }

        public void Dispose()
        {
            _store.Dispose();
            _pool.Dispose();
        }

        public DataReader BeginRead()
        {
            return new DataReader(_pool, _store.BeginRead());
        }

        public DataWriter BeginWrite()
        {
            return new DataWriter(_store.BeginWrite());
        }

        public DataDeleter BeginDelete()
        {
            return new DataDeleter(_store.BeginDelete());
        }
    }
}