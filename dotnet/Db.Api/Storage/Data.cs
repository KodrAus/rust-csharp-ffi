namespace Db.Api.Storage
{
    public sealed class Data
    {
        public Data(string key, dynamic value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public dynamic Value { get; }
    }
}