using System;
using System.Globalization;
using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示进度令牌，可以是字符串或整数。
    /// </summary>
    public readonly struct ProgressToken : IEquatable<ProgressToken>
    {
        /// <summary> 令牌，可以是 string、封装 long 或 null。</summary>
        private readonly object _token;

        /// <summary>使用指定值初始化 <see cref="ProgressToken"/> 的新实例。</summary>
        /// <param name="value">所需的值。</param>
        public ProgressToken(string value)
        {
            _token = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>使用指定值初始化 <see cref="ProgressToken"/> 的新实例。</summary>
        /// <param name="value">所需的值。</param>
        public ProgressToken(long value)
        {
            // 将 long 装箱。实际上，进度令牌几乎总是 string ，因此这种情况应该很少见。
            _token = value;
        }
        
        /// <summary>获取此令牌的值。</summary>
        /// <remarks>这将是 <see cref="string"/>、装箱的 <see cref="long"/> 或 <see langword="null"/>。</remarks>
        public object Token => _token;
        
        /// <inheritdoc />
        public override string ToString() =>
            _token is string stringValue ? stringValue :
            _token is long longValue ? longValue.ToString(CultureInfo.InvariantCulture) :
            null;
        /// <inheritdoc />
        public bool Equals(ProgressToken other) => Equals(_token, other._token);
        
        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ProgressToken other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _token?.GetHashCode() ?? 0;
        
        /// <inheritdoc />
        public static bool operator ==(ProgressToken left, ProgressToken right) => left.Equals(right);

        /// <inheritdoc />
        public static bool operator !=(ProgressToken left, ProgressToken right) => !left.Equals(right);
    }

    public class ProgressTokenNewtonsoftConverter : JsonConverter<ProgressToken>
    {
        public override ProgressToken ReadJson(JsonReader reader, Type objectType, ProgressToken existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String:
                {
                    string s = (string)reader.Value;
                    return new ProgressToken(s);
                }
                case JsonToken.Integer:
                {
                    long l = Convert.ToInt64(reader.Value);
                    return new ProgressToken(l);
                }
                default:
                    // Optionally handle Null token or other types
                    throw new JsonSerializationException("progressToken must be a string or an integer");
            }
        }

        public override void WriteJson(JsonWriter writer, ProgressToken value, JsonSerializer serializer)
        {
            object obj = value.Token;
            switch (obj)
            {
                case string str:
                    writer.WriteValue(str);
                    break;
                case long longValue:
                    writer.WriteValue(longValue);
                    break;
                default:
                    // If null or unexpected, write empty string or null depending on your protocol
                    writer.WriteValue(string.Empty);
                    break;
            }
        }
    }
}