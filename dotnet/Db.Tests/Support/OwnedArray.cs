using System;
using System.Buffers;

namespace Db.Tests.Support
{
    public class OwnedArray : IMemoryOwner<byte>
    {
        private readonly byte[] _array;

        public OwnedArray(byte[] array)
        {
            _array = array;
        }

        public void Dispose()
        {
        }

        public Memory<byte> Memory => _array.AsMemory();
    }
}