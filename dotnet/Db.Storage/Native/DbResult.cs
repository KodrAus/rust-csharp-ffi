using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DbResult
    {
        enum DbResultValue : uint
        {
            Ok,

            Done,
            BufferTooSmall,

            ArgumentNull,
            InternalError
        }

        readonly DbResultValue _result;

        public static (DbResult, string) GetLastResult()
        {
            return LastResult.GetLastResult();
        }

        public void EnsureSuccess()
        {
            if (IsSuccess()) return;
            
            var (lastResult, msg) = GetLastResult();

            // This isn't perfect, but avoids some cases where native calls are made
            // between checking for success.
            if (lastResult._result == _result)
            {
                throw new Exception($"Native storage failed ({_result}), {msg?.TrimEnd()}");
            }

            throw new Exception($"Native storage failed with {_result}");
        }

        public bool IsSuccess()
        {
            return _result == DbResultValue.Ok || _result == DbResultValue.Done;
        }

        public bool IsDone()
        {
            return _result == DbResultValue.Done;
        }

        public bool IsBufferTooSmall()
        {
            return _result == DbResultValue.BufferTooSmall;
        }
    }
}