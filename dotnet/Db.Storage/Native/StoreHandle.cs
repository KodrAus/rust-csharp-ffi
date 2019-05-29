using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    internal class StoreHandle : SafeHandle
    {
        private StoreHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            //if (handle == IntPtr.Zero) return true;

            //var h = handle;
            //handle = IntPtr.Zero;

            Bindings.db_store_close(handle, false);
            return true;
        }
    }
}