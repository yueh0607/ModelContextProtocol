using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端对服务器端 <see cref="RequestMethods.SamplingCreateMessage"/> 的响应。
    /// </summary>
    /// <remarks>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class CreateMessageResult : Result
    {
        /// <summary>
        /// 获取或设置消息内容。
        /// </summary>
        [JsonProperty("content", Required = Required.Always)]
        public ContentBlock Content { get; set; }

        /// <summary>
        /// 获取或设置生成消息的模型的名称。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 这应该包含特定的模型标识符，例如“claude-3-5-sonnet-20241022”或“o3-mini”。
        /// </para>
        /// <para>
        /// 此属性允许服务器知道使用哪个模型生成响应，
        /// 根据模型的功能和特性进行适当的处理。
        /// </para>
        /// </remarks>
        [JsonProperty("model", Required = Required.Always)]
        public string Model { get; set; }

        /// <summary>
        /// 获取或设置消息生成（采样）停止的原因（如果已知）。
        /// </summary>
        /// <remarks>
        /// 常用值包括：
        /// <list type="bullet">
        /// <item><term>endTurn</term><description>模型自然完成了响应。</description></item>
        /// <item><term>maxTokens</term><description>由于达到令牌限制，响应被截断。</description></item>
        /// <item><term>stopSequence</term><description>生成过程中遇到特定的停止序列。</description></item>
        /// </list>
        /// </remarks>
        [JsonProperty("stopReason")]
        public string StopReason { get; set; }

        /// <summary>
        /// Gets or sets the role of the user who generated the message.
        /// </summary>
        [JsonProperty("role", Required = Required.Always)]
        public Role Role { get; set; }
    }
}