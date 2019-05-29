using System;
using System.Buffers;
using Db.Api.Storage;
using Db.Tests.Support;
using Xunit;

namespace Db.Tests.Api
{
    public class HandleTests
    {
        [Fact]
        public void ClosedStoreCannotBeUsedToBeginOperations()
        {
            var tempStore = new TempStore();
            using var store = new DataStore(MemoryPool<byte>.Shared, tempStore.Store);

            tempStore.Dispose();

            Assert.Throws<ObjectDisposedException>(() => store.BeginRead());
            Assert.Throws<ObjectDisposedException>(() => store.BeginWrite());
            Assert.Throws<ObjectDisposedException>(() => store.BeginDelete());
        }
    }
}