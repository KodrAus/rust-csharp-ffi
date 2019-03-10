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

        public void Set(Data data)
        {
            var rawKey = Key.FromString(data.Key);

            var json = JsonConvert.SerializeObject(data.Value);
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            _writer.Set(rawKey, new Span<byte>(jsonBytes));
        }
    }
}