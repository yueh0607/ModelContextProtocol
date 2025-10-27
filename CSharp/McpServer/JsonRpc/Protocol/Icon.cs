using System.Collections.Generic;
using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示一个图标，可用于直观地识别实现、资源、工具或提示。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 图标通过提供视觉上下文并提高可用功能的可发现性来增强用户界面。
    /// 每个图标都包含指向图标资源的源 URI，以及可选的 MIME 类型和大小信息。
    /// </para>
    /// <para>
    /// 支持渲染图标的客户端必须至少支持以下 MIME 类型：
    /// </para>
    /// <list type="bullet">
    /// <item><description>image/png - PNG 图片（安全、通用兼容）</description></item>
    /// <item><description>image/jpeg（和 image/jpg）- JPEG 图片（安全、通用兼容）</description></item>
    /// </list>
    /// <para>
    /// 支持渲染图标的客户端还应支持：
    /// </para>
    /// <list type="bullet">
    /// <item><description>image/svg+xml - SVG 图片（可扩展但需要安全预防措施）</description></item>
    /// <item><description>image/webp - WebP 图片（现代、高效格式）</description></item>
    /// </list>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class Icon
    {
        /// <summary>
        /// 获取或设置指向图标资源的 URI。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 这可以是指向图像文件的 HTTP/HTTPS URL，也可以是包含 base64 编码图像数据的数据 URI。
        /// </para>
        /// <para>
        /// 使用者应采取措施确保提供图标的 URL 与客户端/服务器来自同一域 或受信任的域。
        /// </para>
        /// <para>
        /// 使用者在使用 SVG 时应采取适当的预防措施，因为它们可能包含可执行 JavaScript。
        /// </para>
        /// </remarks>
        [JsonProperty("src", Required = Required.Always)]
        public string Source { get; set; }

        /// <summary>
        /// 获取或设置图标的可选 MIME 类型。
        /// </summary>
        /// <remarks>
        /// 如果服务器的 MIME 类型缺失或为通用类型，则可使用此方法覆盖服务器的 MIME 类型。
        /// 常见值包括“image/png”、“image/jpeg”、“image/svg+xml”和“image/webp”。
        /// </remarks>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        /// <summary>
        /// 获取或设置图标的可选尺寸规格。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 可以指定图标文件可用的一个或多个尺寸。
        /// 示例包括“48x48”，以及适用于 SVG 等可缩放格式的“any”。
        /// </para>
        /// <para>
        /// 如果未提供，客户端应假定图标可以以任意尺寸使用。
        /// </para>
        /// </remarks>
        [JsonProperty("sizes")]
        public IList<string> Sizes { get; set; }

        /// <summary>
        /// 获取或设置此图标的可选主题。
        /// </summary>
        /// <remarks>
        /// 可以是“亮”、“暗”或自定义主题标识符。
        /// 用于指定图标设计的 UI 主题。
        /// </remarks>
        [JsonProperty("theme")]
        public string Theme { get; set; }
    }
}