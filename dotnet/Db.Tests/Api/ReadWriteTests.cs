using System.Buffers;
using System.Linq;
using System.Text;
using Db.Tests.Support;
using Db.Api.Storage;
using Newtonsoft.Json;
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
                new
                {
                    key = Some.KeyWith(3),
                    value = new {title = "Data 1"}
                },
                new
                {
                    key = Some.KeyWith(17),
                    value = new {title = "Data 2"}
                }
            };

            using var tempStore = new TempStore();
            using var store = new DataStore(MemoryPool<byte>.Shared, tempStore.Store);

            using (var writer = store.BeginWrite())
            {
                foreach (var data in events)
                {
                    var json = JsonConvert.SerializeObject(data.value);
                    var utf8Json = Encoding.UTF8.GetBytes(json);

                    writer.Set(new Data(data.key, new OwnedArray(utf8Json)));
                }
            }

            using var reader = store.BeginRead();
            var readData = reader.Data().ToList();

            Assert.Equal(events.Length, readData.Count);

            foreach (var read in readData)
            {
                var expected = events.Single(evt => read.Key == evt.key);

                Assert.Equal(read.Value.GetProperty("title").GetString(), expected.value.title);
            }
        }
    }
}
