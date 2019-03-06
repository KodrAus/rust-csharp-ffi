using System;
using Db.Storage;

namespace Db.Api.Storage
{
    public sealed class DataStore : IDisposable
    {
        private readonly Store _store;

        internal DataStore(Store store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(Store));
        }

        public void Dispose()
        {
            _store.Dispose();
        }

        public DataReader BeginRead()
        {
            return new DataReader(_store.BeginRead());
        }

        public DataWriter BeginWrite()
        {
            return new DataWriter(_store.BeginWrite());
        }
    }
}