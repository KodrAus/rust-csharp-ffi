using Xunit;
using Db.Storage;

namespace Db.Tests.Storage
{
    public class KeyTests
    {
        [Fact]
        public void Equality()
        {
            var a = new Key("abcdefgh-1");
            var b = new Key("abcdefgh-11");

            var c = new Key("abcdefgh-1");
            var d = new Key("abcdefgh-11");

            Assert.True(a == c);
            Assert.Equal(a, c);
            Assert.Equal(b, d);

            Assert.True(a != b);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void ConvertFromString()
        {
            var key = new Key("abcdefgh-42");
            var (hi, lo) = key;

            Assert.Equal("abcdefgh", hi);
            Assert.Equal((ulong) 42, lo);
        }

        [Fact]
        public void ConvertToString()
        {
            var key = new Key("abcdefgh", 42);

            Assert.Equal("abcdefgh-42", key.ToString());
        }

        [Fact]
        public void ReadWriteKey()
        {
            var key = new Key("abcdefgh", 42);
            var (hi, lo) = key;

            Assert.Equal("abcdefgh", hi);
            Assert.Equal((ulong) 42, lo);
        }
    }
}