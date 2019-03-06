using System;
using System.Runtime.CompilerServices;
using System.Text;
using Db.Storage.Native;

namespace Db.Storage
{
    public struct Key
    {
        private static readonly Encoder Encoder = Encoding.ASCII.GetEncoder();

        private DbKey _key;

        internal Key(DbKey key)
        {
            _key = key;
        }

        public Key(string hi, ulong lo)
        {
            unsafe
            {
                var key = default(DbKey);

                var written = Encoder.GetBytes(hi.AsSpan(), new Span<byte>(Unsafe.AsPointer(ref key), 8), true);
                if (written != 8)
                    throw new ArgumentException("The hi string must contain exactly 8 ASCII chars", nameof(hi));

                key.lo = lo;

                _key = key;
            }
        }

        public static Key FromString(string key)
        {
            if (key.Length < 10) throw new ArgumentException("The key is too short", nameof(key));

            var hi = key.Substring(0, 8);
            var lo = Convert.ToUInt64(key.Substring(9));

            return new Key(hi, lo);
        }

        public override string ToString()
        {
            var (hi, lo) = this;

            return $"{hi}-{lo}";
        }

        internal DbKey Value => _key;

        public void Deconstruct(out string hi, out ulong lo)
        {
            unsafe
            {
                hi = Encoding.ASCII.GetString((byte*) Unsafe.AsPointer(ref _key), 8);
                lo = _key.lo;
            }
        }

        public static bool operator ==(Key lhs, Key rhs)
        {
            return lhs._key.hi == rhs._key.hi && lhs._key.lo == rhs._key.lo;
        }

        public static bool operator !=(Key lhs, Key rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Key other)
        {
            return _key.Equals(other._key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }
    }
}