using System;
using Db.Storage;

namespace Db.Api.Storage
{
    public sealed class DataDeleter : IDisposable
    {
        private readonly Deleter _deleter;

        internal DataDeleter(Deleter deleter)
        {
            _deleter = deleter ?? throw new ArgumentNullException(nameof(deleter));
        }

        public void Dispose()
        {
            _deleter.Dispose();
            // The memory pool is borrowed so we don't dispose it
        }

        public void Remove(Key key)
        {
            _deleter.Remove(key);
        }
    }
}