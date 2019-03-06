using System;
using System.Runtime.CompilerServices;
using Db.Storage.Native;

namespace Db.Storage
{
    public sealed class Writer : IDisposable
    {
        private readonly WriterHandle _handle;

        internal Writer(WriterHandle handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public void Dispose()
        {
            _handle.Close();
        }

        public void Set(Key key, Span<byte> value)
        {
            EnsureOpen();

            unsafe
            {
                fixed (void* valuePtr = value)
                {
                    var rawKey = key.Value;
                    var keyPtr = Unsafe.AsPointer(ref rawKey);

                    Bindings.db_write_set(_handle, (IntPtr) keyPtr, (IntPtr) valuePtr, (UIntPtr) value.Length);
                }
            }
        }

        private void EnsureOpen()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(Writer), "The writer has been disposed.");
        }
    }
}