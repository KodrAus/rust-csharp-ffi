using System;
using System.Text;
using Db.Storage;
using Newtonsoft.Json;

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
        }

        public void Remove(string key)
        {
            var rawKey = Key.FromString(key);

            _deleter.Remove(rawKey);
        }
    }
}