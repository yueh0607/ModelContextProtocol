using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// JSON-RPC 协议中的请求消息（Request）。
    /// </summary>
    /// <remarks>
    /// 请求是需要接收方响应的消息。每个请求都包含一个唯一的 ID，
    /// 该 ID 将包含在相应的响应消息（成功响应或错误响应）中。
    ///
    /// 请求消息的接收方应使用提供的参数执行指定的方法，
    /// 并返回结果为 <see cref="JsonRpcResponse"/> 的消息，或返回结果为 <see cref="JsonRpcError"/> 的消息（如果方法执行失败）。
    /// </remarks>
    public sealed class JsonRpcRequest : JsonRpcMessageWithId
    {
        /// <summary>
        /// 要调用的方法的名称。
        /// </summary>
        [JsonProperty("method", Required = Required.Always)]
        public string Method { get; set; }
        
        /// <summary>
        /// 该方法的可选参数。
        /// </summary>
        [JsonProperty("params")]
        public JToken Params { get; set; }
        
        /// <summary>
        /// 创建一个带新的 Id 的 Request 副本
        /// </summary>
        internal JsonRpcRequest WithId(RequestId id)
        {
            return new JsonRpcRequest
            {
                JsonRpc = this.JsonRpc,
                Id = id,
                Method = this.Method,
                Params = this.Params,
            };
        }
    }
}