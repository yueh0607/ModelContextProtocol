using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示 JSON-RPC 协议中的错误响应消息（Error）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当请求无法完成或在处理过程中遇到错误时，会发送错误响应。
    /// 与成功响应一样，错误消息包含与原始请求相同的 ID，以便发送者将错误与其对应的请求进行匹配。
    /// </para>
    /// <para>
    /// 每个错误响应都包含一个结构化的错误详细信息对象，其中包含数字代码、描述性消息，
    /// 以及可选的附加数据，用于提供有关错误的更多上下文信息。
    /// </para>
    /// </remarks>
    public sealed class JsonRpcError : JsonRpcMessageWithId
    {
        /// <summary>
        /// 获取失败请求的详细错误信息，包含错误代码、消息和可选的附加数据
        /// </summary>
        [JsonProperty("error", Required = Required.Always)]
        public JsonRpcErrorDetail Error { get; set; }
    }
}