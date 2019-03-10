using System;
using Db.Storage;

namespace Db.Tests.Support
{
    public class TempStore : IDisposable
    {
        private readonly TempDir _dir;

        public TempStore()
        {
            _dir = new TempDir();
            Store = Store.Open(_dir);
        }

        public Store Store { get; }

        public void Dispose()
        {
            _dir.Dispose();
            Store.Dispose();
        }
    }
}