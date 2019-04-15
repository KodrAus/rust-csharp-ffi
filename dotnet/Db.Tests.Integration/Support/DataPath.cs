using System;
using System.IO;
using System.Linq;

namespace Db.Tests.Integration.Support
{
    public class DataPath : IDisposable
    {
        readonly string _dir;

        public DataPath()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        public static implicit operator string(DataPath @this)
        {
            return @this._dir;
        }

        public override string ToString()
        {
            return _dir;
        }
    }
}