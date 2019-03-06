using System;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    class ReaderHandle : SafeHandle
    {
        ReaderHandle()
            : base(IntPtr.Zero, true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero) return true;

            var h = handle;
            handle = IntPtr.Zero;

            return Bindings.db_read_end(h, check: false).IsSuccess();
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}