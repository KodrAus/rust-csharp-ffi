namespace Db.Api.Storage
{
    public sealed class Data
    {
        public Data(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public object Value { get; }

        public dynamic DynamicValue()
        {
            return Value;
        }
    }
}