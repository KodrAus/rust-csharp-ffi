using System;

namespace Db.Storage
{
    public ref struct ReadResult
    {
        private Key _key;
        private Value _value;
        private int _requiredLength;
        private Result _result;

        private enum Result
        {
            Ok,
            Done,
            BufferTooSmall
        }

        public ref struct Value
        {
            internal Value(Span<byte> buffer, Range range)
            {
                var (start, length) = range.GetOffsetAndLength(buffer.Length);
                Span = buffer.Slice(start, length);
                Range = range;
            }

            public Span<byte> Span { get; }
            public Range Range { get; }
        }

        internal static ReadResult Data(Key key, Span<byte> buffer, Range range)
        {
            return new ReadResult
            {
                _key = key,
                _value = new Value(buffer, range),
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

        public void GetData(out Key key, out Value value)
        {
            if (_result != Result.Ok) throw new InvalidOperationException($"`{nameof(ReadResult)}` has no data.");

            key = _key;
            value = _value;
        }
    }
}
