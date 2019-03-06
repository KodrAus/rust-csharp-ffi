using System;
using System.Text;
using Db.Storage;
using Newtonsoft.Json;

namespace Db.Api.Storage
{
    public sealed class DataWriter : IDisposable
    {
        private readonly Writer _writer;

        internal DataWriter(Writer writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void Set(string key, object value)
        {
            var rawKey = Key.FromString(key);

            var json = JsonConvert.SerializeObject(value);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            _writer.Set(rawKey, new Span<byte>(jsonBytes));
        }
    }
}