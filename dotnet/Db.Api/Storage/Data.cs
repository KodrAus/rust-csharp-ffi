using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
using Db.Storage;

namespace Db.Api.Storage
{
    public sealed class Data : IDisposable
    {
        private static readonly byte[] _keyPropertyName = Encoding.UTF8.GetBytes("key");
        private static readonly byte[] _valuePropertyName = Encoding.UTF8.GetBytes("value");
        private readonly IMemoryOwner<byte> _ownedValueMemory;

        private readonly JsonDocument _value;

        public Data(Key key, IMemoryOwner<byte> value)
        {
            Key = key;
            RawValue = value.Memory;

            _ownedValueMemory = value;
            _value = JsonDocument.Parse(RawValue);
        }

        public Data(Key key, IMemoryOwner<byte> value, Range range)
        {
            Key = key;

            var (start, length) = range.GetOffsetAndLength(value.Memory.Length);
            RawValue = value.Memory.Slice(start, length);

            _ownedValueMemory = value;
            _value = JsonDocument.Parse(RawValue);
        }

        public Key Key { get; }

        public JsonElement Value => _value.RootElement;
        internal ReadOnlyMemory<byte> RawValue { get; }

        public void Dispose()
        {
            _value.Dispose();
            _ownedValueMemory?.Dispose();
        }

        public void WriteAsValue(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString(_keyPropertyName, Key.ToString());

            writer.WritePropertyName(_valuePropertyName);
            Value.WriteTo(writer);

            writer.WriteEndObject();
        }
    }
}
