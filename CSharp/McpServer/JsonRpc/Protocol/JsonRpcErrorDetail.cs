using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示 JSON-RPC 错误响应的详细错误信息。
    /// </summary>
    /// <remarks>
    /// 此类用作 <see cref="JsonRpcError"/> 消息的一部分，用于在请求无法完成时提供结构化的错误信息。
    /// JSON-RPC 2.0 规范定义了错误响应的标准格式，其中包含数字代码、人类可读的消息，以及可选的附加数据。
    /// </remarks>
    public sealed class JsonRpcErrorDetail
    {
        /// <summary>
        /// 根据 JSON-RPC 规范获取整数错误代码。
        /// </summary>
        [JsonProperty("code", Required = Required.Always)]
        public int Code { get; set; }

        /// <summary>
        /// 获取错误的简短描述。
        /// </summary>
        /// <remarks>
        /// 这应该是对错误原因的简短、易读的解释。
        /// 对于标准错误代码，建议使用 JSON-RPC 2.0 规范中定义的描述。
        /// </remarks>
        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; }

        /// <summary>
        /// 获取可选的附加错误数据。
        /// </summary>
        /// <remarks>
        /// 此属性可以包含任何可能有助于客户端理解或解决错误的附加信息。
        /// 常见示例包括验证错误、堆栈跟踪（在开发环境中）或有关错误条件的上下文信息。
        /// </remarks>
        [JsonProperty("data")]
        public object Data { get; set; }
    }
}