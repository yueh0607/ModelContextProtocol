using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器发出的消息，用于通过客户端从用户处获取更多信息。
    /// </summary>
    public sealed class ElicitRequestParams
    {
        /// <summary>
        /// 获取或设置要呈现给用户的消息。
        /// </summary>
        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置请求的架构。
        /// </summary>
        /// <remarks>
        /// 可以是 <see cref="StringSchema"/>、<see cref="NumberSchema"/>、<see cref="BooleanSchema"/> 或 <see cref="EnumSchema"/> 之一。
        /// </remarks>
        [JsonProperty("requestedSchema")]
        public RequestSchema RequestedSchema { get; set; } = new RequestSchema();


        /// <summary>表示引出请求中使用的请求模式。</summary>
        public class RequestSchema
        {
            /// <summary>获取架构的类型。</summary>
            /// <remarks>始终为“object”。</remarks>
            [JsonProperty("type")]
            public string Type => "object";

            /// <summary>获取或设置架构的属性。</summary>
            [JsonProperty("properties")]
            public IDictionary<string, PrimitiveSchemaDefinition> Properties { get; set; }
                = new Dictionary<string, PrimitiveSchemaDefinition>();

            /// <summary>获取或设置架构所需的属性。</summary>
            [JsonProperty("required")]
            public IList<string> Required { get; set; }

        }
    }
    /// <summary>
    /// 表示 JSON Schema 的受限子集：
    /// <see cref="StringSchema"/>、<see cref="NumberSchema"/>、<see cref="BooleanSchema"/> 或 <see cref="EnumSchema"/>。
    /// 抽象基类，用于字符串 / 数字 / 布尔 / 枚举的 schema
    /// </summary>
    [JsonConverter(typeof(PrimitiveSchemaDefinitionNewtonsoftConverter))]
    public abstract class PrimitiveSchemaDefinition // TODO：由于缺乏对 AllowOutOfOrderMetadataProperties 的下级支持，因此存在此转换器。
    {
        protected PrimitiveSchemaDefinition() { }

        /// <summary>获取架构的类型。</summary>
        [JsonProperty("type")]
        public abstract string Type { get; set; }

        /// <summary>获取或设置架构的标题。</summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>获取或设置架构的描述。</summary>
        [JsonProperty("description")]
        public string Description { get; set; }
    }

    /// <summary>
    /// 为 <see cref="ResourceContents"/> 提供 <see cref="JsonConverter"/>。
    /// 和原生的 JsonConverter 不同，它支持根据 "type" 字段动态反序列化为不同的子类。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PrimitiveSchemaDefinitionNewtonsoftConverter : JsonConverter<PrimitiveSchemaDefinition>
    {
        /// <inheritdoc/>
        public override PrimitiveSchemaDefinition ReadJson(JsonReader reader, Type objectType, PrimitiveSchemaDefinition existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonSerializationException("Expected StartObject.");
            }

            JObject obj = JObject.Load(reader);

            var type = obj.Value<string>("type");
            if (type == null) throw new JsonSerializationException("Field 'type' is required in PrimitiveSchemaDefinition");

            PrimitiveSchemaDefinition schema;
            switch (type)
            {
                case "string":
                    // 判断是否为枚举类型（有 "enum" 字段）
                    if (obj.TryGetValue("enum", out JToken enumToken))
                    {
                        var es = new EnumSchema
                        {
                            Enum = enumToken.ToObject<IList<string>>(serializer)
                        };

                        if (obj.TryGetValue("enumNames", out JToken namesToken))
                            es.EnumNames = namesToken.ToObject<IList<string>>(serializer);
                        schema = es;
                    }
                    else
                    {
                        schema = new StringSchema
                        {
                            MinLength = obj.Value<int?>("minLength"),
                            MaxLength = obj.Value<int?>("maxLength"),
                            Format = obj.Value<string>("format")
                        };
                    }
                    break;

                case "number":
                case "integer":
                    schema = new NumberSchema
                    {
                        Minimum = obj.Value<double?>("minimum"),
                        Maximum = obj.Value<double?>("maximum")
                    };
                    break;

                case "boolean":
                    schema = new BooleanSchema
                    {
                        Default = obj.Value<bool?>("default")
                    };
                    break;

                default:
                    throw new JsonSerializationException($"Unknown schema type: {type}");
            }

            // 公共属性赋值
            schema.Title = obj.Value<string>("title");
            schema.Description = obj.Value<string>("description");

            return schema;
        }

        public override void WriteJson(JsonWriter writer, PrimitiveSchemaDefinition value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);

            if (value.Title != null)
            {
                writer.WritePropertyName("title");
                writer.WriteValue(value.Title);
            }
            if (value.Description != null)
            {
                writer.WritePropertyName("description");
                writer.WriteValue(value.Description);
            }

            switch (value)
            {
                case StringSchema ss:
                    if (ss.MinLength.HasValue)
                    {
                        writer.WritePropertyName("minLength");
                        writer.WriteValue(ss.MinLength.Value);
                    }
                    if (ss.MaxLength.HasValue)
                    {
                        writer.WritePropertyName("maxLength");
                        writer.WriteValue(ss.MaxLength.Value);
                    }
                    if (ss.Format != null)
                    {
                        writer.WritePropertyName("format");
                        writer.WriteValue(ss.Format);
                    }
                    break;

                case NumberSchema ns:
                    if (ns.Minimum.HasValue)
                    {
                        writer.WritePropertyName("minimum");
                        writer.WriteValue(ns.Minimum.Value);
                    }
                    if (ns.Maximum.HasValue)
                    {
                        writer.WritePropertyName("maximum");
                        writer.WriteValue(ns.Maximum.Value);
                    }
                    break;

                case BooleanSchema bs:
                    if (bs.Default.HasValue)
                    {
                        writer.WritePropertyName("default");
                        writer.WriteValue(bs.Default.Value);
                    }
                    break;

                case EnumSchema es:
                    writer.WritePropertyName("enum");
                    serializer.Serialize(writer, es.Enum);
                    if (es.EnumNames != null)
                    {
                        writer.WritePropertyName("enumNames");
                        serializer.Serialize(writer, es.EnumNames);
                    }
                    break;

                default:
                    throw new JsonSerializationException($"Unsupported schema type: {value.GetType().Name}");
            }

            writer.WriteEndObject();
        }
    }
    /// <summary>表示字符串类型的模式。</summary>
    public class StringSchema : PrimitiveSchemaDefinition
    {
        /// <inheritdoc/>
        public override string Type
        {
            get => "string";
            set
            {
                if (value != "string")
                    throw new ArgumentException("Type must be 'string'.", nameof(value));
            }
        }

        /// <summary>获取或设置字符串的最小长度。</summary>
        [JsonProperty("minLength")]
        public int? MinLength { get; set; }

        /// <summary>获取或设置字符串的最大长度。</summary>
        [JsonProperty("maxLength")]
        public int? MaxLength { get; set; }



        private string _format;

        /// <summary>获取或设置字符串的特定格式（“email”、“uri”、“date”或“date-time”）。</summary>
        [JsonProperty("format")]
        public string Format
        {
            get => _format;
            set
            {
                // 允许 null，或者是协议允许的那些字符串
                if (value != null && value != "email" && value != "uri" && value != "date" && value != "date-time")
                {
                    throw new ArgumentException("Format must be 'email', 'uri', 'date', or 'date-time'.", nameof(value));
                }
                _format = value;
            }
        }
    }

    /// <summary>表示数字或整数类型的模式。</summary>
    public class NumberSchema : PrimitiveSchemaDefinition
    {
        private string _type;
        /// <inheritdoc/>
        public override string Type
        {
            get => _type ?? (_type = "number");
            set
            {
                if (value != "number" && value != "integer")
                    throw new ArgumentException("Type must be 'number' or 'integer'.", nameof(value));
                _type = value;
            }
        }

        /// <summary>获取或设置允许的最小值。</summary>
        [JsonProperty("minimum")]
        public double? Minimum { get; set; }

        /// <summary>获取或设置允许的最大值。</summary>
        [JsonProperty("maximum")]
        public double? Maximum { get; set; }
    }

    /// <summary>表示布尔类型的模式。</summary>
    public class BooleanSchema : PrimitiveSchemaDefinition
    {
        /// <inheritdoc/>
        public override string Type
        {
            get => "boolean";
            set
            {
                if (value != "boolean")
                    throw new ArgumentException("Type must be 'boolean'.", nameof(value));
            }
        }
        /// <summary>获取或设置布尔值的默认值。</summary>
        [JsonProperty("default")]
        public bool? Default { get; set; }
    }

    /// <summary>表示枚举类型的模式。</summary>
    public class EnumSchema : PrimitiveSchemaDefinition
    {
        /// <inheritdoc/>
        public override string Type
        {
            get => "string";
            set
            {
                if (value != "string")
                    throw new ArgumentException("Type must be 'string'.", nameof(value));
            }
        }
        /// <summary>获取或设置枚举允许的字符串值列表。</summary>
        [JsonProperty("enum")]
        public IList<string> Enum { get; set; } = new List<string>();

        /// <summary>获取或设置与枚举值对应的可选显示名称。</summary>
        [JsonProperty("enumNames")]
        public IList<string> EnumNames { get; set; }
    }

}