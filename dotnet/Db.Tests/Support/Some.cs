using System.Text;
using System.Threading;
using Db.Storage;

namespace Db.Tests.Support
{
    public static class Some
    {
        private static long _last;

        public static long Long()
        {
            return Interlocked.Increment(ref _last);
        }

        public static int Int()
        {
            return (int) Long();
        }

        public static ulong ULong()
        {
            return (ulong) Long();
        }

        public static Key KeyWith(ulong id)
        {
            return new Key("testdoc1", id);
        }

        public static (Key, byte[]) Event()
        {
            var payload = "Hello, world #" + Int() + "!";
            return EventWith(ULong(), payload);
        }

        public static (Key, byte[]) EventWith(ulong id, string payload)
        {
            var pch = Encoding.UTF8.GetBytes(payload);
            return (KeyWith(id), pch);
        }
    }
}