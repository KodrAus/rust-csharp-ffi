using System;
using System.Text;
using System.Runtime.CompilerServices;
using Db.Storage.Native;

namespace Db.Storage
{
    public struct Key
    {
        static readonly Encoder _encoder = Encoding.ASCII.GetEncoder();

        DbKey _key;

        public Key(string hi, ulong lo)
        {
            unsafe
            {
                var key = default(DbKey);

                var hiPtr = Unsafe.AsPointer(ref key);
                var loPtr = Unsafe.Add<byte>(hiPtr, 8);

                var written = _encoder.GetBytes(hi.AsSpan(), new Span<byte>(hiPtr, 8), true);
                if (written != 8)
                {
                    throw new ArgumentException("The hi string must contain exactly 8 ASCII chars", nameof(hi));
                }
                
                Unsafe.Write(loPtr, lo);
                
                _key = key;
            }
        }

        internal Key(DbKey key)
        {
            _key = key;
        }

        public void Deconstruct(out string hi, out ulong lo)
        {
            unsafe
            {
                var hiPtr = Unsafe.AsPointer(ref this);
                var loPtr = Unsafe.Add<byte>(hiPtr, 8);

                hi = Encoding.ASCII.GetString((byte*)hiPtr, 8);
                lo = Unsafe.Read<ulong>(loPtr);
            }
        }
    }
}