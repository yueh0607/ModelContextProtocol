using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    [JsonConverter(typeof(RequestIdNewtonsoftConverter))]
    public readonly struct RequestId : IEquatable<RequestId>
    {
        /// <summary>id，可以是 string、装箱的 long 或 null。</summary>
        private readonly object _id;


        /// <summary>使用指定值初始化 <see cref="RequestId"/> 的新实例。</summary>
        /// <param name="value">所需的 ID 值。</param>
        public RequestId(string value)
        {
            _id = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>使用指定值初始化 <see cref="RequestId"/> 的新实例。</summary>
        /// <param name="value">所需的 ID 值。</param>
        public RequestId(long value)
        {
            // 将 long 数据装箱。实际应用中，请求 ID 几乎都是字符串，所以这种情况应该很少见。
            _id = value;
        }

        /// <summary>获取此 ID 对应的底层对象。</summary>
        /// <remarks>这将是 <see cref="string"/>、装箱的 <see cref="long"/> 或 <see langword="null"/>。</remarks>
        public object Id => _id;

        /// <inheritdoc />
        public override string ToString() =>
            _id is string stringValue ? stringValue :
            _id is long longValue ? longValue.ToString(CultureInfo.InvariantCulture) :
            string.Empty;

        /// <inheritdoc />
        public bool Equals(RequestId other) => Equals(_id, other._id);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is RequestId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _id?.GetHashCode() ?? 0;

        public static bool operator ==(RequestId left, RequestId right) => left.Equals(right);

        public static bool operator !=(RequestId left, RequestId right) => !left.Equals(right);

    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RequestIdNewtonsoftConverter : JsonConverter<RequestId>
    {
        public override RequestId ReadJson(JsonReader reader, Type objectType, RequestId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                    {
                        string s = (string)reader.Value;
                        return new RequestId(s);
                    }
                case JsonToken.Integer:
                    {
                        // 注意：Json.NET 的整数可以从 Int64 / Int32 等多种类型来
                        long l = Convert.ToInt64(reader.Value);
                        return new RequestId(l);
                    }
                default:
                    throw new JsonSerializationException("RequestId must be string or integer.");
            }
        }

        public override void WriteJson(JsonWriter writer, RequestId value, JsonSerializer serializer)
        {
            // 根据内部类型写出
            switch (value.Id)
            {
                case string s:
                    writer.WriteValue(s);
                    break;
                case long l:
                    writer.WriteValue(l);
                    break;
                default:
                    // 若为 null 或未知类型，则写空字符串或 null 判断你自己要的策略
                    writer.WriteValue(string.Empty);
                    break;
            }
        }
    }

}