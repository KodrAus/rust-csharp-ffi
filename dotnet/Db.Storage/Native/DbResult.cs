using System;
using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DbResult
    {
        private enum Kind : uint
        {
            Ok,

            Done,
            BufferTooSmall,

            ArgumentNull,
            InternalError
        }

        private readonly Kind _result;
        private readonly uint _id;

        public static (DbResult, string) GetLastResult()
        {
            return LastResult.GetLastResult();
        }

        internal DbResult Check()
        {
            if (IsSuccess() || IsBufferTooSmall()) return this;

            var (lastResult, msg) = GetLastResult();

            // Check whether the last result kind and id are the same
            // We need to use both because successful results won't
            // bother setting the id (it avoids some synchronization)
            if (lastResult._result == _result && lastResult._id == _id)
                throw new Exception($"Native storage failed ({_result}), {msg?.TrimEnd()}");

            throw new Exception($"Native storage failed with {_result}");
        }

        public bool IsSuccess()
        {
            return _result == Kind.Ok || _result == Kind.Done;
        }

        public bool IsDone()
        {
            return _result == Kind.Done;
        }

        public bool IsBufferTooSmall()
        {
            return _result == Kind.BufferTooSmall;
        }
    }
}