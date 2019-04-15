using Autofac;
using System.Buffers;
using Db.Api.Storage;
using Db.Storage;

namespace Db.Api
{
    class DataModule : Module
    {
        readonly string _dataPath;

        public DataModule(string dataPath)
        {
            _dataPath = dataPath;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(ctx => new DataStore(MemoryPool<byte>.Shared, Store.Open(_dataPath)))
                .As<DataStore>()
                .SingleInstance();
        }
    }
}