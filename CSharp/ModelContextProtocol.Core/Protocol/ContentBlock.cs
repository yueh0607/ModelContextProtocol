using System;
using ModelContextProtocol.Json;
using ModelContextProtocol.Json.Linq;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议 (MCP) 中的内容。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ContentBlock"/> 类是 MCP 中的基础类型，可以基于 <see cref="Type"/> 属性表示不同形式的内容。
    /// 派生类型如 <see cref="TextContentBlock"/>、<see cref="ImageContentBlock"/> 和 <see cref="EmbeddedResourceBlock"/> 提供特定类型的内容。
    /// </para>
    /// <para>
    /// 此类在整个 MCP 中用于表示消息、工具响应、以及客户端和服务器之间的其他通信中的内容。
    /// </para>
    /// <para>
    /// 更多详情，请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    [JsonConverter(typeof(ContentBlockNewtonsoftConverter))] // TODO：由于缺乏对 AllowOutOfOrderMetadataProperties 的下级支持，因此存在此转换器。
    public abstract class ContentBlock
    {
        protected ContentBlock()
        {
        }
        /// <summary>
        /// 获取或设置内容类型。
        /// </summary>
        /// <remarks>
        /// 此项决定了内容对象的结构。有效值包括“image”、“audio”、“text”、“resource”和“resource_link”。
        /// </remarks>
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置内容的可选注释。
        /// </summary>
        /// <remarks>
        /// 这些注释可用于指定目标受众（<see cref="Role.User"/>、<see cref="Role.Assistant"/> 或两者）以及内容的优先级。
        /// 客户端可以使用此信息来筛选或确定不同角色内容的优先级。
        /// </remarks>
        [JsonProperty("annotations")]
        public Annotations Annotations { get; set; }

        // 注意：在原始版本中，一些派生类除了基类的 Result /Meta 处理之外，还重新声明了 Meta。
        // 如果 Meta 是一个公共字段，您可以将其放在基类中或进行适当的派生。


    }

    /// <summary>
    /// 为 <see cref="ContentBlock"/> 提供 <see cref="JsonConverter"/>。
    /// </summary>
    public class ContentBlockNewtonsoftConverter : JsonConverter<ContentBlock>
    {
        public override ContentBlock ReadJson(JsonReader reader, Type objectType, ContentBlock existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // Load JObject from reader
            JObject obj = JObject.Load(reader);

            // Determine type
            var type = obj.Value<string>("type");
            if (type == null)
                throw new JsonSerializationException("Missing 'type' property for ContentBlock");

            ContentBlock result;
            switch (type)
            {
                case "text":
                    result = new TextContentBlock
                    {
                        Text = obj.Value<string>("text") ??
                               throw new JsonSerializationException("Text is required for text content."),
                        Meta = obj.Value<JObject>("_meta")
                    };
                    break;

                case "image":
                    result = new ImageContentBlock
                    {
                        Data = obj.Value<string>("data") ??
                               throw new JsonSerializationException("Data is required for image content."),
                        MimeType = obj.Value<string>("mimeType") ??
                                   throw new JsonSerializationException("MimeType is required for image content."),
                        Meta = obj.Value<JObject>("_meta")
                    };
                    break;

                case "audio":
                    result = new AudioContentBlock
                    {
                        Data = obj.Value<string>("data") ??
                               throw new JsonSerializationException("Data is required for audio content."),
                        MimeType = obj.Value<string>("mimeType") ??
                                   throw new JsonSerializationException("MimeType is required for audio content."),
                        Meta = obj.Value<JObject>("_meta")
                    };
                    break;

                case "resource":
                    result = new EmbeddedResourceBlock
                    {
                        Resource = obj["resource"]?.ToObject<ResourceContents>(serializer) ??
                                   throw new JsonSerializationException("Resource is required for resource content."),
                        Meta = obj.Value<JObject>("_meta")
                    };
                    break;

                case "resource_link":
                    result = new ResourceLinkBlock
                    {
                        Uri = obj.Value<string>("uri") ??
                              throw new JsonSerializationException("Uri is required for resource_link content."),
                        Name = obj.Value<string>("name") ??
                               throw new JsonSerializationException("Name is required for resource_link content."),
                        Description = obj.Value<string>("description"),
                        MimeType = obj.Value<string>("mimeType"),
                        Size = obj.Value<long?>("size")
                    };
                    break;

                default:
                    throw new JsonSerializationException($"Unknown content type: '{type}'");
            }

            // Common: annotations if present
            if (obj.TryGetValue("annotations", out JToken annToken))
            {
                result.Annotations = annToken.ToObject<Annotations>(serializer);
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, ContentBlock value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

            writer.WritePropertyName("type");
            writer.WriteValue(value.Type);

            switch (value)
            {
                case TextContentBlock t:
                    writer.WritePropertyName("text");
                    writer.WriteValue(t.Text);
                    if (t.Meta != null)
                    {
                        writer.WritePropertyName("_meta");
                        serializer.Serialize(writer, t.Meta);
                    }

                    break;

                case ImageContentBlock img:
                    writer.WritePropertyName("data");
                    writer.WriteValue(img.Data);
                    writer.WritePropertyName("mimeType");
                    writer.WriteValue(img.MimeType);
                    if (img.Meta != null)
                    {
                        writer.WritePropertyName("_meta");
                        serializer.Serialize(writer, img.Meta);
                    }

                    break;

                case AudioContentBlock aud:
                    writer.WritePropertyName("data");
                    writer.WriteValue(aud.Data);
                    writer.WritePropertyName("mimeType");
                    writer.WriteValue(aud.MimeType);
                    if (aud.Meta != null)
                    {
                        writer.WritePropertyName("_meta");
                        serializer.Serialize(writer, aud.Meta);
                    }

                    break;

                case EmbeddedResourceBlock emb:
                    writer.WritePropertyName("resource");
                    serializer.Serialize(writer, emb.Resource);
                    if (emb.Meta != null)
                    {
                        writer.WritePropertyName("_meta");
                        serializer.Serialize(writer, emb.Meta);
                    }

                    break;

                case ResourceLinkBlock link:
                    writer.WritePropertyName("uri");
                    writer.WriteValue(link.Uri);
                    writer.WritePropertyName("name");
                    writer.WriteValue(link.Name);
                    if (link.Description != null)
                    {
                        writer.WritePropertyName("description");
                        writer.WriteValue(link.Description);
                    }

                    if (link.MimeType != null)
                    {
                        writer.WritePropertyName("mimeType");
                        writer.WriteValue(link.MimeType);
                    }

                    if (link.Size.HasValue)
                    {
                        writer.WritePropertyName("size");
                        writer.WriteValue(link.Size.Value);
                    }

                    break;
            }

            if (value.Annotations != null)
            {
                writer.WritePropertyName("annotations");
                serializer.Serialize(writer, value.Annotations);
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>表示提供给或来自 LLM 的文本。</summary>
    public sealed class TextContentBlock : ContentBlock
    {
        /// <summary>初始化 <see cref="TextContentBlock"/> 类的实例。</summary>
        public TextContentBlock() => Type = "text";

        /// <summary>
        /// 获取或设置消息的文本内容。
        /// </summary>
        [JsonProperty("text", Required = Required.Always)]
        public string Text { get; set; }

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
    /// 表示提供给或来自 LLM 的图像。
    /// </summary>
    public sealed class ImageContentBlock : ContentBlock
    {
        /// <summary>初始化 <see cref="ImageContentBlock"/> 类的实例。</summary>
        public ImageContentBlock() => Type = "image";

        /// <summary>
        /// 获取或设置 base64 编码的图像数据。
        /// </summary>
        [JsonProperty("data", Required = Required.Always)]
        public string Data { get; set; }

        /// <summary>
        /// 获取或设置内容的 MIME 类型（或“媒体类型”），指定数据的格式。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 常见值包括“image/png”和“image/jpeg”。
        /// </para>
        /// </remarks>
        [JsonProperty("mimeType", Required = Required.Always)]
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
    /// 表示提供给或来自 LLM 的音频。
    /// </summary>
    public sealed class AudioContentBlock : ContentBlock
    {
        /// <summary>初始化 <see cref="AudioContentBlock"/> 类的实例。</summary>
        public AudioContentBlock() => Type = "audio";

        /// <summary>
        /// 获取或设置 base64 编码的音频数据。
        /// </summary>
        [JsonProperty("data", Required = Required.Always)]
        public string Data { get; set; }

        /// <summary>
        /// 获取或设置内容的 MIME 类型（或“媒体类型”），指定数据的格式。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 常见值包括“audio/wav”和“audio/mp3”。
        /// </para>
        /// </remarks>
        [JsonProperty("mimeType", Required = Required.Always)]
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

    /// <summary>表示嵌入到提示或工具调用结果中的资源内容。</summary>
    /// <remarks>
    /// 如何最佳呈现嵌入资源以满足 LLM 和/或用户的需求，由客户端自行决定。
    /// </remarks>
    public sealed class EmbeddedResourceBlock : ContentBlock
    {
        /// <summary>Initializes the instance of the <see cref="ResourceLinkBlock"/> class.</summary>
        public EmbeddedResourceBlock() => Type = "resource";

        /// <summary>
        /// 当 <see cref="Type"/> 为“resource”时，获取或设置消息的资源内容。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 资源可以是文本格式 (<see cref="TextResourceContents"/>) 或
        /// 二进制格式 (<see cref="BlobResourceContents"/>)，从而实现灵活的数据表示。
        /// 每个资源都有一个 URI，可用于识别和检索。
        /// </para>
        /// </remarks>
        [JsonProperty("resource", Required = Required.Always)]
        public ResourceContents Resource { get; set; }

        /// <summary>
        /// 获取或设置 MCP 为协议级元数据保留的元数据。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容做出假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }
    }

    /// <summary>表示服务器能够读取的资源，包含在提示或工具调用结果中。</summary>
    /// <remarks>
    /// 工具返回的资源链接不保证会出现在 `resources/list` 请求的结果中。
    /// </remarks>
    public sealed class ResourceLinkBlock : ContentBlock
    {
        /// <summary>初始化 <see cref="ResourceLinkBlock"/> 类的实例。</summary>
        public ResourceLinkBlock() => Type = "resource_link";

        /// <summary>
        /// 获取或设置此资源的 URI。
        /// </summary>
        [JsonProperty("uri", Required = Required.Always)]
        public string Uri { get; set; }

        /// <summary>
        /// 获取或设置此资源的可读名称。
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置此资源所代表内容的描述。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 客户端可以使用它来增强 LLM 对可用资源的理解。它可以被视为对模型的“提示”。
        /// </para>
        /// <para>
        /// 描述应提供有关资源内容、格式和用途的清晰上下文。
        /// 这有助于 AI 模型更好地决策何时访问或引用该资源。
        /// </para>
        /// <para>
        /// 客户端应用程序还可以将此描述用于用户界面中的显示目的，或帮助用户了解可用资源。
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置此资源的 MIME 类型。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="MimeType"/> 指定资源内容的格式，帮助客户端正确解释和显示数据。
        /// 常见的 MIME 类型包括：纯文本的“text/plain”、PDF 文档的“application/pdf”、
        /// PNG 图像的“image/png”和 JSON 数据的“application/json”。
        /// </para>
        /// <para>
        /// 如果 MIME 类型未知或不适用于资源，则此属性可能为 <see langword="null"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        /// <summary>
        /// 获取或设置原始资源内容的大小（Base64 编码之前），以字节为单位（如果已知）。
        /// </summary>
        /// <remarks>
        /// 应用程序可以使用它来显示文件大小并估算上下文窗口的使用情况。
        /// </remarks>
        [JsonProperty("size")]
        public long? Size { get; set; }
    }
}