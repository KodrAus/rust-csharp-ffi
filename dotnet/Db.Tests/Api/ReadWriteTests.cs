using System.Buffers;
using System.Linq;
using Db.Tests.Support;
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
                new Data(Some.KeyWith(3).ToString(), new {title = "Data 1"}),
                new Data(Some.KeyWith(17).ToString(), new {title = "Data 2"})
            };

            using (var tempStore = new TempStore())
            using (var store = new DataStore(MemoryPool<byte>.Shared, tempStore.Store))
            {
                using (var writer = store.BeginWrite())
                {
                    foreach (var data in events) writer.Set(data);
                }

                using (var reader = store.BeginRead())
                {
                    var readData = reader.Data().ToList();

                    Assert.Equal(events.Length, readData.Count());

                    foreach (dynamic read in readData)
                    {
                        var expected = events.Single(evt => read.Key == evt.Key);

                        Assert.Equal((string) read.Value.title, (string) expected.DynamicValue().title);
                    }
                }
            }
        }
    }
}