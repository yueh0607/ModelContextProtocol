using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器对客户端发出的 <see cref="RequestMethods.PromptsGet"/> 请求的响应。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GetPromptResult"/> 包含服务器生成的提示消息列表，这些消息可以直接用于与 AI 模型交互。
    /// 每条消息都包含特定的角色（用户或助手）和内容（文本、图像或其他格式）。
    /// </para>
    /// <para>
    /// 为了与 AI 客户端库集成，可以使用扩展方法 <see cref="AIContentExtensions.ToChatMessages"/> 将
    /// <see cref="GetPromptResult"/> 转换为 <see cref="ChatMessage"/> 对象集合。（没有实现）
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public sealed class GetPromptResult : Result
    {
        /// <summary>
        /// 获取或设置提示的可选描述。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此描述提供有关提示目的和使用场景的上下文信息。
        /// 它有助于开发人员理解提示的设计用途以及应如何使用。
        /// </para>
        /// <para>
        /// 当服务器响应 <see cref="RequestMethods.PromptsGet"/> 请求返回此结果时，
        /// 客户端应用程序可以使用此描述来提供有关提示的上下文信息，或在用户界面中显示。
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置服务器提供的提示消息列表。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此列表包含组成提示的所有消息。每条消息都有特定的角色（用户或助手）
        /// 和内容块，这些内容块可以包含文本、图像或嵌入的资源引用。
        /// </para>
        /// <para>
        /// 这些消息按照它们在列表中出现的顺序，构成了与 AI 模型的完整对话上下文。
        /// </para>
        /// </remarks>
        [JsonProperty("messages", Required = Required.Always)]
        public IList<PromptMessage> Messages { get; set; } = new List<PromptMessage>();
    }
}