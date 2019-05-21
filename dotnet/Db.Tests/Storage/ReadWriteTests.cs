using System;
using System.Linq;
using Db.Tests.Support;
using Db.Storage;
using Xunit;

namespace Db.Tests.Storage
{
    public class ReadWriteTests
    {
        [Fact]
        public void WrittenDataCanBeRead()
        {
            var events = new[]
            {
                Some.Event(),
                Some.Event()
            };

            using (var store = new TempStore())
            {
                using (var writer = store.Store.BeginWrite())
                {
                    foreach (var (key, payload) in events) writer.Set(key, payload);
                }

                var count = 0;

                using (var reader = store.Store.BeginRead())
                {
                    // We just about guarantee the first read will be too small
                    var readInto = new byte[1];

                    ReadResult read;
                    while (!(read = reader.TryReadNext(readInto.AsSpan())).IsDone)
                    {
                        if (read.IsBufferTooSmall(out var required))
                        {
                            readInto = new byte[required];
                            continue;
                        }

                        read.GetData(out var key, out var payload);
                        count += 1;

                        var (_, foundPayload) = events.Single(evt => key == evt.Item1);

                        Assert.Equal(payload.Span.ToArray(), foundPayload.ToArray());
                    }
                }

                Assert.Equal(events.Length, count);
            }
        }
    }
}