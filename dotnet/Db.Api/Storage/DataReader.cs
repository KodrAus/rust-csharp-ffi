using System;
using System.Buffers;
using System.Collections.Generic;
using Db.Storage;

namespace Db.Api.Storage
{
    public sealed class DataReader : IDisposable
    {
        private readonly MemoryPool<byte> _pool;
        private readonly Reader _reader;

        internal DataReader(MemoryPool<byte> pool, Reader reader)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public void Dispose()
        {
            _reader.Dispose();
            // The memory pool is borrowed so we don't dispose it
        }

        public IEnumerable<Data> Data()
        {
            var requiredSize = 1024;
            while (true)
            {
                var readInto = _pool.Rent(requiredSize);
                ReadResult read;

                try
                {
                    read = _reader.TryReadNext(readInto.Memory.Span);
                }
                catch
                {
                    readInto.Dispose();
                    throw;
                }

                // If the read is done then return
                if (read.IsDone)
                {
                    readInto.Dispose();
                    yield break;
                }

                // If the buffer is too small then resize and try again
                if (read.IsBufferTooSmall(out var required))
                {
                    requiredSize = required;
                    readInto.Dispose();
                    continue;
                }

                // If we get this far then we've read an event
                // Ownership of the rented buffer is transferred
                // to the returned `Data`
                read.GetData(out var key, out var payload);
                yield return new Data(key, readInto, payload.Range);
            }
        }
    }
}