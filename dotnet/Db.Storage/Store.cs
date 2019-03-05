using System;
using Db.Storage.Native;

namespace Db.Storage
{
    public class Store : IDisposable
    {
        StoreHandle _handle;

        public static Store Open()
        {
            Bindings.db_store_open(out var handle).EnsureSuccess();

            return new Store
            {
                _handle = handle
            };
        }

        public bool IsOpen => !_handle.IsClosed;

        public void Close()
        {
            Dispose();
        }

        public void Dispose() => _handle.Close();
    }
}