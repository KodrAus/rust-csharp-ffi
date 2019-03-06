using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct DbKey
    {
        fixed byte Data[16];
    }
}