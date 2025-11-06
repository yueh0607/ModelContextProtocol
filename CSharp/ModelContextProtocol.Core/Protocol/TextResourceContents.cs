using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议中资源的文本内容。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当需要通过模型上下文协议交换文本数据时，使用 <see cref="TextResourceContents"/>。
    /// 文本直接存储在 <see cref="Text"/> 属性中。
    /// </para>
    /// <para>
    /// 此类继承自 <see cref="ResourceContents"/>，后者也有一个兄弟实现，
    /// 用于二进制资源的 <see cref="BlobResourceContents"/>。处理资源时，
    /// 根据内容的性质选择合适的类型。
    /// </para>
    /// <para>
    /// 更多详情，请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class TextResourceContents : ResourceContents
    {
        /// <summary>
        /// 获取或设置项目的文本。
        /// </summary>
        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }
}