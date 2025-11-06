using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示在模型上下文协议中向 LLM API 发送或从 LLM API 接收的消息。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SamplingMessage"/> 封装了在模型上下文协议中发送到 AI 模型或从 AI 模型接收的内容。
    /// 每条消息都有一个特定的角色（<see cref="Role.User"/> 或 <see cref="Role.Assistant"/>），并包含可以是文本或图像的内容。
    /// </para>
    /// <para>
    /// <see cref="SamplingMessage"/> 对象通常用于 <see cref="CreateMessageRequestParams"/> 中的集合中，
    /// 用于表示 LLM 采样的提示或查询。它们构成了模型上下文协议中文本生成请求的核心数据结构。
    /// /// </para>
    /// <para>
    /// 虽然与 <see cref="PromptMessage"/> 类似，
    /// 但 <see cref="SamplingMessage"/> 专注于直接 LLM 采样操作，
    /// 而不是 <see cref="PromptMessage"/> 提供的增强资源嵌入功能。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class SamplingMessage
    {
        /// <summary>
        /// 获取或设置消息内容。
        /// </summary>
        [JsonProperty("content", Required = Required.Always)]
        public ContentBlock Content { get; set; }

        /// <summary>
        /// 获取或设置消息发送者的角色，指示消息来自“ user ”还是“ assistant ”。
        /// </summary>
        [JsonProperty("role", Required = Required.Always)]
        public Role Role { get; set; }
    }
}