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

        public void Set(Key key, ReadOnlySpan<byte> value)
        {
            unsafe
            {
                var rawKey = key.Value;
                var keyPtr = Unsafe.AsPointer(ref rawKey);

                fixed (byte* valuePtr = value)
                {
                    Bindings.db_write_set(_handle, (IntPtr) keyPtr, (IntPtr) valuePtr, (UIntPtr) value.Length);
                }
            }
        }

        public void Dispose()
        {
            _handle.Dispose();
        }
    }
}