using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示模型上下文协议中资源的二进制内容。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当需要通过模型上下文协议交换二进制数据时，使用 <see cref="BlobResourceContents"/>。
    /// 二进制数据在 <see cref="Blob"/> 属性中表示为 base64 编码的字符串。
    /// </para>
    /// <para>
    /// 此类继承自 <see cref="ResourceContents"/>，后者还有一个兄弟实现，
    /// 用于基于文本的资源的 <see cref="TextResourceContents"/>。
    /// 处理资源时，根据内容的性质选择合适的类型。
    /// </para>
    /// <para>
    /// 更多详情，请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class BlobResourceContents : ResourceContents
    {
        /// <summary>
        /// 获取或设置表示项目二进制数据的 base64 编码字符串。
        /// </summary>
        [JsonProperty("blob")]
        public string Blob { get; set; } = string.Empty;
    }
}