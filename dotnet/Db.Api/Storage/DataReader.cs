using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Db.Storage;
using Newtonsoft.Json;

namespace Db.Api.Storage
{
    public sealed class DataReader : IDisposable
    {
        private readonly Reader _reader;
        private readonly MemoryPool<byte> _pool;

        internal DataReader(MemoryPool<byte> pool, Reader reader)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public IEnumerable<Data> Data()
        {
            var requiredSize = 1024;
            while (true)
                using (var readInto = _pool.Rent(requiredSize))
                {
                    var read = _reader.TryReadNext(readInto.Memory.Span);

                    // If the read is done then return
                    if (read.IsDone) yield break;

                    // If the buffer is too small then resize and try again
                    if (read.IsBufferTooSmall(out var required))
                    {
                        requiredSize = required;
                        continue;
                    }

                    // Get the data for the read event
                    read.GetData(out var key, out var payload);

                    yield return new Data(key.ToString(),
                        JsonConvert.DeserializeObject(Encoding.UTF8.GetString(payload)));
                }
        }
    }
}