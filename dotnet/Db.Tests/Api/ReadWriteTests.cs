using System;
using System.Buffers;
using System.Linq;
using Db.Tests.Support;
using Db.Storage;
using Db.Api.Storage;
using Xunit;

namespace Db.Tests.Api
{
    public class ReadWriteTests
    {
        [Fact]
        public void WrittenDataCanBeRead()
        {
            var events = new[]
            {
                (Some.KeyWith(3).ToString(), new { title = "Data 1" }),
                (Some.KeyWith(17).ToString(), new { title = "Data 2" })
            };

            using (var tempStore = new TempStore())
            using (var store = new DataStore(MemoryPool<byte>.Shared, tempStore.Store))
            {
                using (var writer = store.BeginWrite())
                {
                    foreach (var (key, value) in events) writer.Set(key, value);
                }

                using (var reader = store.BeginRead())
                {
                    var readData = reader.Data().ToList();

                    Assert.Equal(events.Length, readData.Count());

                    foreach (dynamic read in readData)
                    {
                        var (_, foundValue) = events.Single(evt => read.Key == evt.Item1);

                        Assert.Equal((string)read.Value.title, (string)foundValue.title);
                    }
                }
            }
        }
    }
}