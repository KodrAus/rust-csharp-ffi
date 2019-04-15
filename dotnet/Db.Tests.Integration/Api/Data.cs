namespace Db.Tests.Integration.Api
{
    class Data
    {
        public Data(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public object Value { get; set; }

        public dynamic DynamicValue => Value;
    }
}