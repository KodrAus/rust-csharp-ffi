using System;
using System.Security.Permissions;
using System.Text;

namespace Db.Storage.Native
{
    static class LastResult
    {
        public static (DbResult, string) GetLastResult()
        {
            return FillLastResult(new Span<byte>(new byte[1024]));
        }

        private static unsafe (DbResult, string) FillLastResult(Span<byte> buffer)
        {
            fixed (byte* messageBufPtr = buffer)
            {
                var result = Bindings.db_last_result(
                    (IntPtr) messageBufPtr,
                    (UIntPtr) buffer.Length,
                    out var actualMessageLen,
                    out var lastResult);

                if (result.IsBufferTooSmall()) return FillLastResult(new Span<byte>(new byte[(int) actualMessageLen]));

                return (lastResult, Encoding.UTF8.GetString(messageBufPtr, (int) actualMessageLen));
            }
        }
    }
}
