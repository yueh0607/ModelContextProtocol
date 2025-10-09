using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示 JSON-RPC 协议中的 Notification 消息。
    /// </summary>
    /// <remarks>
    /// Notification 是不需要 Response 且不与 Response 消息匹配的消息。
    /// 它们适用于单向通信，例如日志通知和进度更新。
    /// 与 Request 不同，Notification 不包含 ID 字段，因为没有与之匹配的 Response。
    /// </remarks>   
    public class JsonRpcNotification : JsonRpcMessage
    {
        /// <summary>
        /// 获取或设置 Notification 方法的名称。
        /// </summary>
        [JsonProperty("method", Required = Required.Always)]
        public string Method { get; set; }

        /// <summary>
        /// 获取或设置 Notification 的可选参数。
        /// </summary>
        [JsonProperty("params")]
        public JToken Params { get; set; }
    }
}