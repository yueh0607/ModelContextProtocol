using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议 (MCP) 系统中的消息，用于客户端与 AI 模型之间的通信。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PromptMessage"/> 封装了在模型上下文协议中发送到 AI 模型或从 AI 模型接收的内容。
    /// 每条消息都有一个特定的角色（<see cref="Role.User"/> 或 <see cref="Role.Assistant"/>），并包含可以是
    /// 文本、图像、音频或嵌入资源的内容。
    /// </para>
    /// <para>
    /// 此类与 <see cref="SamplingMessage"/> 类似，但对嵌入来自 MCP 服务器的资源提供了增强支持。
    /// 它在 MCP 消息交换流程中充当核心数据结构，特别是在提示形成和模型响应中。
    /// </para>
    /// <para>
    /// <see cref="PromptMessage"/> 对象通常在 <see cref="GetPromptResult"/> 的集合中使用，
    /// 以表示完整的对话或提示序列。它们可以使用扩展方法 <see cref="AIContentExtensions.ToChatMessage(PromptMessage)"/> 和
    /// <see cref="AIContentExtensions.ToPromptMessages(ChatMessage)"/>（未实现）与 <see cref="ChatMessage"/>（未实现）
    /// 对象进行相互转换。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public sealed class PromptMessage
    {
        /// <summary>
        /// 获取或设置消息的内容，可以是文本、图像、音频或嵌入的资源。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Content"/> 对象包含所有消息负载，无论是简单的文本、
        /// base64 编码的二进制数据（用于图像/音频），还是对嵌入资源的引用。
        /// <see cref="ContentBlock.Type"/> 属性指示特定的内容类型。
        /// </para>
        /// <para>
        /// 如果未明确设置，默认使用空的文本内容块。
        /// </para>
        /// </remarks>
        [JsonProperty("content", Required = Required.Always)]
        public ContentBlock Content { get; set; } = new TextContentBlock { Text = string.Empty };

        /// <summary>
        /// 获取或设置消息发送者的角色，指定是来自"user"（用户）还是"assistant"（助手）。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 在模型上下文协议中，每条消息必须具有明确的角色分配，以维护对话流程。
        /// 用户消息表示来自用户的查询或输入，而助手消息表示由 AI 模型生成的响应。
        /// </para>
        /// <para>
        /// 如果未明确设置，默认角色为 <see cref="Role.User"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("role", Required = Required.Always)]
        public Role Role { get; set; } = Role.User;
    }
}