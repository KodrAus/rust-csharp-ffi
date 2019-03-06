using System.Runtime.InteropServices;

namespace Db.Storage.Native
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    struct DbKey
    {
        internal ulong hi;
        internal ulong lo;

        internal bool Equals(DbKey other)
        {
            return hi == other.hi && lo == other.lo;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is DbKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (hi.GetHashCode() * 397) ^ lo.GetHashCode();
            }
        }
    }
}