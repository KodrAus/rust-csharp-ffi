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
        static extern DbResult _db_last_result(
            IntPtr messageBuf, 
            UIntPtr messageBufLen,
            out UIntPtr actualMessageLen,
            out DbResult lastResult);
        public static DbResult db_last_result(
            IntPtr messageBuf, 
            UIntPtr messageBufLen,
            out UIntPtr actualMessageLen,
            out DbResult lastResult)
        {
            return _db_last_result(messageBuf, messageBufLen, out actualMessageLen, out lastResult).Check();
        }

        [DllImport(NativeLibrary, EntryPoint = "db_store_open", ExactSpelling = true)]
        static extern DbResult _db_store_open(IntPtr path, UIntPtr pathLen, out StoreHandle store);
        public static DbResult db_store_open(IntPtr path, UIntPtr pathLen, out StoreHandle store)
        {
            return _db_store_open(path, pathLen, out store).Check();
        }

        [DllImport(NativeLibrary, EntryPoint = "db_store_close", ExactSpelling = true)]
        static extern DbResult _db_store_close(IntPtr store);
        public static DbResult db_store_close(IntPtr store)
        {
            return _db_store_close(store).Check();
        }
    }
}
