using System;
using System.Text;
using Db.Storage.Native;

namespace Db.Storage
{
    public sealed class Store : IDisposable
    {
        private StoreHandle _handle;
        private string _path;

        public static Store Open(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var pathUtf8 = Encoding.UTF8.GetBytes(path);

            unsafe
            {
                fixed (byte* pathUtf8Ptr = pathUtf8)
                {
                    Bindings.db_store_open((IntPtr) pathUtf8Ptr, (UIntPtr) pathUtf8.Length, out var handle);

                    return new Store
                    {
                        _path = path,
                        _handle = handle
                    };
                }
            }
        }

        public Reader BeginRead()
        {
            Bindings.db_read_begin(_handle, out var readerHandle);
            return new Reader(readerHandle);
        }

        public Writer BeginWrite()
        {
            Bindings.db_write_begin(_handle, out var writerHandle);
            return new Writer(writerHandle);
        }

        public Deleter BeginDelete()
        {
            Bindings.db_delete_begin(_handle, out var deleterHandle);
            return new Deleter(deleterHandle);
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}