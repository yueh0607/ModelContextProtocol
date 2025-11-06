using System;
using System.ComponentModel;

using ModelContextProtocol.Json;
using ModelContextProtocol.Json.Linq;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 提供一个表示模型上下文协议中资源内容的基类。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ResourceContents"/> 用作可通过模型上下文协议交换的不同类型资源的基类。
    /// 资源由 URI 标识，可以包含 不同类型的数据。
    /// </para>
    /// <para>
    /// 此类是抽象类，有两个具体实现：
    /// <list type="bullet">
    /// <item><description><see cref="TextResourceContents"/> - 用于基于文本的资源</description></item>
    /// <item><description><see cref="BlobResourceContents"/> - 用于二进制数据资源</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// 有关更多详细信息，请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(ResourceContentsNewtonsoftConverter))]
    public abstract class ResourceContents
    {
        protected ResourceContents()
        {
        }
        /// <summary>
        /// 获取或设置资源的 URI。
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置资源内容的 MIME 类型。
        /// </summary>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        /// <summary>
        /// 获取或设置 MCP 为协议级元数据保留的元数据。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容做出假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }

    }
    /// <summary>
    /// 为 <see cref="ResourceContents"/> 提供 <see cref="JsonConverter"/>。
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ResourceContentsNewtonsoftConverter : JsonConverter<ResourceContents>
    {
        /// <inheritdoc/>
        public override ResourceContents ReadJson(JsonReader reader, Type objectType, ResourceContents existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException();

            JObject obj = JObject.Load(reader);

            string uri = obj.Value<string>("uri") ?? throw new JsonSerializationException("Missing uri for ResourceContents");
            string mimeType = obj.Value<string>("mimeType");  // may be null
            JObject meta = obj.Value<JObject>("_meta");

            // 根据 blob 或 text 字段的存在来决定子类
            if (obj.TryGetValue("blob", out JToken blobToken))
            {
                return new BlobResourceContents
                {
                    Uri = uri,
                    MimeType = mimeType,
                    Blob = blobToken.ToObject<string>(serializer),
                    Meta = meta
                };
            }
            else if (obj.TryGetValue("text", out JToken textToken))
            {
                return new TextResourceContents
                {
                    Uri = uri,
                    MimeType = mimeType,
                    Text = textToken.ToObject<string>(serializer),
                    Meta = meta
                };
            }

            // Neither blob nor text present => ambiguous or invalid
            return null;
        }

        // <inheritdoc/>
        public override void WriteJson(JsonWriter writer, ResourceContents value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("uri");
            writer.WriteValue(value.Uri);

            if (value.MimeType != null)
            {
                writer.WritePropertyName("mimeType");
                writer.WriteValue(value.MimeType);
            }

            switch (value)
            {
                case BlobResourceContents blob:
                    writer.WritePropertyName("blob");
                    writer.WriteValue(blob.Blob);
                    break;

                case TextResourceContents txt:
                    writer.WritePropertyName("text");
                    writer.WriteValue(txt.Text);
                    break;
            }

            if (value.Meta != null)
            {
                writer.WritePropertyName("_meta");
                serializer.Serialize(writer, value.Meta);
            }

            writer.WriteEndObject();
        }
    }
}