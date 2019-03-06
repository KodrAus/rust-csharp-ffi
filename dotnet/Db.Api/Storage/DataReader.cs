using System;
using System.Collections.Generic;
using System.Text;
using Db.Storage;
using Newtonsoft.Json;

namespace Db.Api.Storage
{
    public sealed class DataReader : IDisposable
    {
        private readonly Reader _reader;

        internal DataReader(Reader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public IEnumerable<dynamic> Data()
        {
            var readInto = new byte[1024];

            ReadResult read;
            while (!(read = _reader.TryReadNext(readInto.AsSpan())).IsDone)
            {
                if (read.IsBufferTooSmall(out var required))
                {
                    readInto = new byte[required];
                    continue;
                }

                read.GetData(out var key, out var payload);

                yield return new
                {
                    key = key.ToString(),
                    value = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(payload))
                };
            }
        }
    }
}