using System;
using System.Security;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    static class Bindings
    {
#if AOT
        const string NativeLibrary = "*";
#elif WINDOWS
        const string NativeLibrary = "Native/dbc.dll";
#elif LINUX
        const string NativeLibrary = "Native/libdbc.so";
#elif MACOS
        private const string NativeLibrary = "Native/libdbc.dylib";
#endif

        [DllImport(NativeLibrary, EntryPoint = "db_last_result", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_last_result(
            IntPtr messageBuf,
            UIntPtr messageBufLen,
            out UIntPtr actualMessageLen,
            out DbResult lastResult);

        public static DbResult db_last_result(
            IntPtr messageBuf,
            UIntPtr messageBufLen,
            out UIntPtr actualMessageLen,
            out DbResult lastResult,
            bool check = true)
        {
            return MaybeCheck(_db_last_result(messageBuf, messageBufLen, out actualMessageLen, out lastResult), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_store_open", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_store_open(IntPtr path, UIntPtr pathLen, out StoreHandle store);

        public static DbResult db_store_open(IntPtr path, UIntPtr pathLen, out StoreHandle store, bool check = true)
        {
            return MaybeCheck(_db_store_open(path, pathLen, out store), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_store_close", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern DbResult _db_store_close(IntPtr store);

        public static DbResult db_store_close(IntPtr store, bool check = true)
        {
            return MaybeCheck(_db_store_close(store), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_read_begin", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_read_begin(StoreHandle store, out ReaderHandle reader);

        public static DbResult db_read_begin(StoreHandle store, out ReaderHandle reader, bool check = true)
        {
            return MaybeCheck(_db_read_begin(store, out reader), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_read_next", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_read_next(
            ReaderHandle reader,
            out DbKey key,
            IntPtr valueBuf,
            UIntPtr valueBufLen,
            out UIntPtr actualValueLen);

        public static DbResult db_read_next(
            ReaderHandle reader,
            out DbKey key,
            IntPtr valueBuf,
            UIntPtr valueBufLen,
            out UIntPtr actualValueLen,
            bool check = true)
        {
            return MaybeCheck(_db_read_next(reader, out key, valueBuf, valueBufLen, out actualValueLen), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_read_end", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern DbResult _db_read_end(IntPtr reader);

        public static DbResult db_read_end(IntPtr reader, bool check = true)
        {
            return MaybeCheck(_db_read_end(reader), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_write_begin", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_write_begin(StoreHandle store, out WriterHandle writer);

        public static DbResult db_write_begin(StoreHandle store, out WriterHandle writer, bool check = true)
        {
            return MaybeCheck(_db_write_begin(store, out writer), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_write_set", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_write_set(
            WriterHandle writer,
            IntPtr key,
            IntPtr value,
            UIntPtr valueLen);

        public static DbResult db_write_set(
            WriterHandle writer,
            IntPtr key,
            IntPtr value,
            UIntPtr valueLen,
            bool check = true)
        {
            return MaybeCheck(_db_write_set(writer, key, value, valueLen), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_write_end", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern DbResult _db_write_end(IntPtr writer);

        public static DbResult db_write_end(IntPtr writer, bool check = true)
        {
            return MaybeCheck(_db_write_end(writer), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_delete_begin", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_delete_begin(StoreHandle store, out DeleterHandle deleter);

        public static DbResult db_delete_begin(StoreHandle store, out DeleterHandle deleter, bool check = true)
        {
            return MaybeCheck(_db_delete_begin(store, out deleter), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_delete_remove", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        private static extern DbResult _db_delete_remove(
            DeleterHandle deleter,
            IntPtr key);

        public static DbResult db_delete_remove(
            DeleterHandle deleter,
            IntPtr key,
            bool check = true)
        {
            return MaybeCheck(_db_delete_remove(deleter, key), check);
        }

        [DllImport(NativeLibrary, EntryPoint = "db_delete_end", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
        private static extern DbResult _db_delete_end(IntPtr deleter);

        public static DbResult db_delete_end(IntPtr deleter, bool check = true)
        {
            return MaybeCheck(_db_delete_end(deleter), check);
        }

        private static DbResult MaybeCheck(DbResult result, bool check)
        {
            return check ? result.Check() : result;
        }
    }
}
