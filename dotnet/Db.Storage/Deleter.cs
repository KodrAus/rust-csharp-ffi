using System;
using System.Security.Permissions;
using System.Runtime.CompilerServices;
using Db.Storage.Native;

namespace Db.Storage
{
    public sealed class Deleter : IDisposable
    {
        private readonly DeleterHandle _handle;

        internal Deleter(DeleterHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public void Remove(Key key)
        {
            EnsureOpen();

            unsafe
            {
                var rawKey = key.Value;
                var keyPtr = Unsafe.AsPointer(ref rawKey);
                
                Bindings.db_delete_remove(_handle, (IntPtr) keyPtr);
            }
        }

        private void EnsureOpen()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(Deleter), "The deleter has been disposed.");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!_handle.IsInvalid)
            {
                _handle.Dispose();
            }
        }
    }
}
