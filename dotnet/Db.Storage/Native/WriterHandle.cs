using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    class WriterHandle : SafeHandle
    {
        private WriterHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero) return true;

            var h = handle;
            handle = IntPtr.Zero;

            Bindings.db_write_end(h, false);
            return true;
        }
    }
}