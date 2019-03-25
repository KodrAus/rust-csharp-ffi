using System;
using System.Text;
using Db.Storage.Native;

namespace Db.Storage
{
    public sealed class Store : IDisposable
    {
        private StoreHandle _handle;
        private string _path;

        public bool IsOpen => !_handle.IsClosed;

        public void Dispose()
        {
            _handle.Close();
        }

        public static Store Open(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var pathUtf8 = Encoding.UTF8.GetBytes(path);

            unsafe
            {
                fixed (void* p = pathUtf8)
                {
                    Bindings.db_store_open((IntPtr) p, (UIntPtr) (pathUtf8?.Length ?? 0), out var handle);

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
            EnsureOpen();

            Bindings.db_read_begin(_handle, out var readerHandle);
            return new Reader(readerHandle);
        }

        public Writer BeginWrite()
        {
            EnsureOpen();

            Bindings.db_write_begin(_handle, out var writerHandle);
            return new Writer(writerHandle);
        }

        public Deleter BeginDelete()
        {
            EnsureOpen();

            Bindings.db_delete_begin(_handle, out var deleterHandle);
            return new Deleter(deleterHandle);
        }

        public void Close()
        {
            Dispose();
        }

        private void EnsureOpen()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(Store), $"The store at `{_path}` has been disposed.");
        }
    }
}