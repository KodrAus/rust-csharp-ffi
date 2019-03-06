using System;
using Xunit;

namespace Db.Storage.Tests
{
    public class KeyTests
    {
        [Fact]
        public void ReadWriteKey()
        {
            var key = new Key("abcdefgh", 42);
            var (hi, lo) = key;

            Assert.Equal("abcdefgh", hi);
            Assert.Equal((ulong)42, lo);
        }
    }
}
