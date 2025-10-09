using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示模型上下文协议 (MCP) 中使用的 JSON-RPC 消息，其中包含 ID。
    /// </summary>
    /// <remarks>
    /// 在 JSON-RPC 协议中，带有 ID 的消息需要接收方的响应。
    /// 这包括请求消息（期望匹配的响应）和响应消息
    /// （其中包含其响应的原始请求的 ID）。
    /// ID 用于将请求与其响应关联起来，从而允许异步
    /// 通信，即无需等待响应即可发送多个请求。
    /// </remarks>
    public abstract class JsonRpcMessageWithId : JsonRpcMessage
    {
        /// <summary>
        /// 构造函数，保护可见性。
        /// </summary>
        protected JsonRpcMessageWithId()
        {
        }
        /// <summary>
        /// 获取消息 Id
        /// </summary>
        /// <remarks>
        /// 每个 ID 在给定会话的上下文中必须是唯一的。
        /// </remarks>
        [JsonProperty("id")]
        public RequestId Id { get; set; }
    }
}