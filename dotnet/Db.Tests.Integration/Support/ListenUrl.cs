using System.Threading;

namespace Db.Tests.Integration.Support
{
    class ListenUrl
    {
        static int NextPort = 50000;
        readonly string _value;

        public ListenUrl()
        {
            _value = $"http://localhost:{GetNextPort()}";
        }

        static int GetNextPort()
        {
            return Interlocked.Increment(ref NextPort);
        }

        public static implicit operator string(ListenUrl @this)
        {
            return @this._value;
        }

        public override string ToString()
        {
            return _value;
        }
    }
}