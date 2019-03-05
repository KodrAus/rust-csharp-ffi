using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    static class Bindings
    {
#if AOT
        const string NativeLibrary = "*";
#elif WINDOWS
        const string NativeLibrary = "Native/db.dll";
#elif LINUX
        const string NativeLibrary = "Native/libdb.so";
#elif MACOS
        const string NativeLibrary = "Native/libdb.dylib";
#endif

        [DllImport(NativeLibrary, EntryPoint = "db_last_result", ExactSpelling = true)]
        public static extern DbResult db_last_result(
            IntPtr messageBuf, 
            UIntPtr messageBufLen,
            out UIntPtr actualMessageLen,
            out DbResult lastResult);

        [DllImport(NativeLibrary, EntryPoint = "db_store_open", ExactSpelling = true)]
        public static extern DbResult db_store_open(out StoreHandle store);

        [DllImport(NativeLibrary, EntryPoint = "db_store_close", ExactSpelling = true)]
        public static extern DbResult db_store_close(IntPtr store);
    }
}
