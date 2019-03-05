using System;
using System.Text;
using Db.Storage.Native;

namespace Db.Storage
{
    public class Store : IDisposable
    {
        StoreHandle _handle;

        public static Store Open(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var pathUtf8 = Encoding.UTF8.GetBytes(path);

            unsafe
            {
                fixed (void* p = pathUtf8)
                {
                    Bindings.db_store_open((IntPtr)p, (UIntPtr)(pathUtf8?.Length ?? 0), out var handle);

                    return new Store
                    {
                        _handle = handle
                    };
                }
            }
        }

        public bool IsOpen => !_handle.IsClosed;

        public void Close()
        {
            Dispose();
        }

        public void Dispose() => _handle.Close();
    }
}