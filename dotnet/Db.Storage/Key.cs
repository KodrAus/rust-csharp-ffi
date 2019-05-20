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
                var keyPtr = Unsafe.AsPointer(ref key);

                var written = Encoder.GetBytes(hi.AsSpan(), new Span<byte>(keyPtr, 8), true);
                if (written != 8)
                    throw new ArgumentException("The hi string must contain exactly 8 ASCII chars", nameof(hi));

                Unsafe.WriteUnaligned(Unsafe.Add<ulong>(keyPtr, 1), lo);

                _key = key;
            }
        }

        public static Key FromString(string key)
        {
            if (key.Length < 10) throw new ArgumentException("The key is too short", nameof(key));
            if (key[8] != '-') throw new Exception("The key is in an invalid format");

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
            var local = _key;

            unsafe
            {
                var localPtr = Unsafe.AsPointer(ref local);

                hi = Encoding.ASCII.GetString((byte*) localPtr, 8);
                lo = Unsafe.ReadUnaligned<ulong>(Unsafe.Add<ulong>(localPtr, 1));
            }
        }

        public static bool operator ==(Key lhs, Key rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Key lhs, Key rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Key other)
        {
            var local = _key;

            unsafe
            {
                var localPtr = Unsafe.AsPointer(ref local);
                var otherPtr = Unsafe.AsPointer(ref other._key);

                var thisSpan = new Span<byte>(localPtr, 16);
                var otherSpan = new Span<byte>(otherPtr, 16);

                return thisSpan.SequenceEqual(otherSpan);
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode()
        {
            var local = this;

            unsafe
            {
                var localPtr = Unsafe.AsPointer(ref local);

                var hi = Unsafe.ReadUnaligned<ulong>(localPtr);
                var lo = Unsafe.ReadUnaligned<ulong>(Unsafe.Add<ulong>(localPtr, 1));

                return (hi.GetHashCode() * 397) ^ lo.GetHashCode();
            }
        }
    }
}