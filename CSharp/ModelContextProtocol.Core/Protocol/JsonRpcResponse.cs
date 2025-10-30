using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// JSON-RPC 协议中的成功响应消息（Response）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 响应消息用于回复请求消息，并包含方法执行的结果。
    /// 每个响应都包含与原始请求相同的 ID，以便发送者将响应与相应的请求进行匹配。
    /// </para>
    /// <para>
    /// 此类表示带有结果的成功响应。有关错误响应，请参阅 <see cref="JsonRpcError"/>。
    /// </para>
    /// </remarks>
    public sealed class JsonRpcResponse : JsonRpcMessageWithId
    {
        /// <summary>
        /// 获取方法调用的结果。
        /// </summary>
        /// <remarks>
        /// 此属性包含服务器响应 JSON-RPC 方法请求返回的结果数据。
        /// </remarks>
        [JsonProperty("result")]
        public JObject Result { get; set; }
    }
}