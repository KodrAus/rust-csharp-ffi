using System;
using Db.Storage;

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
            // The memory pool is borrowed so we don't dispose it
        }

        public void Set(Data data)
        {
            _writer.Set(data.Key, data.RawValue.Span);
        }
    }
}