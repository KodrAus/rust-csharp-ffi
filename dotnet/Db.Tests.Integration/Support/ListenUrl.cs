using System.Threading;

namespace Db.Tests.Integration.Support
{
    internal class ListenUrl
    {
        private static int _nextPort = 50000;
        private readonly string _value;

        public ListenUrl()
        {
            _value = $"http://localhost:{GetNextPort()}";
        }

        private static int GetNextPort()
        {
            return Interlocked.Increment(ref _nextPort);
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