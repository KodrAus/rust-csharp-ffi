using System;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    class StoreHandle : SafeHandle
    {
        StoreHandle()
            : base(IntPtr.Zero, true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero) return true;

            var h = handle;
            handle = IntPtr.Zero;

            return Bindings.db_store_close(h, check: false).IsSuccess();
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}