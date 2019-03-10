using System;
using System.IO;
using System.Linq;

namespace Db.Tests.Support
{
    public class TempDir : IDisposable
    {
        private readonly string _dir;

        public TempDir()
        {
            _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        public static implicit operator string(TempDir @this)
        {
            return @this._dir;
        }

        public string GetFullPath(params string[] subComponents)
        {
            return Path.Combine(new[] {_dir}.Concat(subComponents).ToArray());
        }
    }
}