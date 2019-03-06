using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct DbKey
    {
        internal fixed byte _data[16];
    }
}