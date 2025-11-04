using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器能够读取的已知资源。
    /// </summary>
    /// <remarks>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public class Resource : IBaseMetadata
    {
        /// <inheritdoc />
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the URI of this resource.
        /// </summary>
        [JsonProperty("uri", Required = Required.Always)]
        public string Uri { get; set; }

        /// <summary>
        /// 获取或设置此资源所代表内容的描述。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 客户端可利用此描述提升大型语言模型对可用资源的理解能力。可将其视为向模型提供的“提示”。
        /// </para>
        /// <para>
        /// 描述应清晰说明资源的内容、格式及用途。
        /// 这有助于AI模型更准确地判断何时访问或引用该资源。
        /// </para>
        /// <para>
        /// 客户端应用程序也可将此描述用于用户界面展示
        /// 或帮助用户理解可用资源。
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置此资源的 MIME 类型。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="MimeType"/> 指定资源内容的格式，帮助客户端正确解析和显示数据。
        /// 常见 MIME 类型包括：纯文本的 “text/plain”、PDF 文档的 “application/pdf”、
        /// PNG 图像的 “image/png” 以及 JSON 数据的 “application/json”。
        /// </para>
        /// <para>
        /// 若资源的 MIME 类型未知或不适用，此属性可能为 <see langword="null"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        /// <summary>
        /// 获取或设置资源的可选注释。
        /// </summary>
        /// <remarks>
        /// 这些注释可用于指定目标受众（<see cref="Role.User"/>、<see cref="Role.Assistant"/> 或两者兼有）
        /// 以及资源的优先级。客户端可利用此信息为不同角色过滤或优先处理资源。
        /// </remarks>
        [JsonProperty("annotations")]
        public Annotations Annotations { get; set; }

        /// <summary>
        /// 获取或设置原始资源内容（在 Base64 编码前）的大小（以字节为单位），若已知。
        /// </summary>
        /// <remarks>
        /// 应用程序可利用此属性显示文件大小并估算上下文窗口的使用情况。
        /// </remarks>
        [JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// 获取或设置此资源的可选图标列表。
        /// </summary>
        /// <remarks>
        /// 客户端可使用此功能在用户界面中显示资源的图标。
        /// </remarks>
        [JsonProperty("icons")]
        public IList<Icon> Icons { get; set; }

        /// <summary>
        /// 获取或设置由 MCP 为协议级元数据保留的元数据。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容进行任何假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }

        // /// <summary>
        // /// Gets or sets the callable server resource corresponding to this metadata if any.
        // /// </summary>
        // [JsonIgnore]
        // public McpServerResource? McpServerResource { get; set; }
    }
}