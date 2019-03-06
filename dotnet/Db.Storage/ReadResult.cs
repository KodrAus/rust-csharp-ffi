using System;

namespace Db.Storage
{
    public ref struct ReadResult
    {
        private Key _key;
        private Span<byte> _value;
        private int _requiredLength;
        private Result _result;

        private enum Result
        {
            Ok,
            Done,
            BufferTooSmall
        }

        internal static ReadResult Data(Key key, Span<byte> value)
        {
            return new ReadResult
            {
                _key = key,
                _value = value,
                _result = Result.Ok
            };
        }

        internal static ReadResult Done()
        {
            return new ReadResult
            {
                _result = Result.Done
            };
        }

        internal static ReadResult BufferTooSmall(int requiredLength)
        {
            return new ReadResult
            {
                _requiredLength = requiredLength,
                _result = Result.BufferTooSmall
            };
        }

        public bool IsBufferTooSmall(out int required)
        {
            if (_result != Result.BufferTooSmall)
            {
                required = 0;
                return false;
            }

            required = _requiredLength;
            return true;
        }

        public bool IsDone => _result == Result.Done;

        public bool HasValue => _result == Result.Ok;

        public void GetData(out Key key, out Span<byte> value)
        {
            if (_result != Result.Ok) throw new InvalidOperationException($"`{nameof(ReadResult)}` has no data.");

            key = _key;
            value = _value;
        }
    }
}