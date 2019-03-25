using System;
using System.Buffers;
using System.Linq;
using Db.Tests.Support;
using Db.Storage;
using Db.Api.Storage;
using Xunit;

namespace Db.Tests.Api
{
    public class DeleteTests
    {
        [Fact]
        public void DeletedDataCannotBeRead()
        {
            var deletedKey = Some.KeyWith(3).ToString();

            var events = new[]
            {
                new Data(deletedKey, new { title = "Data 1" }),
                new Data(Some.KeyWith(17).ToString(), new { title = "Data 2" })
            };

            using (var tempStore = new TempStore())
            using (var store = new DataStore(MemoryPool<byte>.Shared, tempStore.Store))
            {
                using (var writer = store.BeginWrite())
                {
                    foreach (var data in events) writer.Set(data);
                }
                
                using (var deleter = store.BeginDelete())
                {
                    deleter.Remove(deletedKey);
                }

                using (var reader = store.BeginRead())
                {
                    var readData = reader.Data().ToList();

                    Assert.Equal(1, readData.Count());
                    Assert.False(readData.Any(data => data.Key == deletedKey));
                }
            }
        }
    }
}