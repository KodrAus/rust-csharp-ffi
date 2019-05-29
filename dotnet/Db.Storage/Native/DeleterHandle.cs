using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    class DeleterHandle : SafeHandle
    {
        private DeleterHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero) return true;

            var h = handle;
            handle = IntPtr.Zero;

            Bindings.db_delete_end(h, false);
            return true;
        }
    }
}