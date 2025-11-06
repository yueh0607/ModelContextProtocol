using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示在协议握手期间，客户端向服务器发送的 <see cref="RequestMethods.Initialize"/> 请求所使用的参数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="InitializeRequestParams"/> 是模型上下文协议通信流程中发送的第一条消息。
    /// 它建立客户端和服务器之间的连接，协商协议版本，并声明客户端的功能。
    /// </para>
    /// <para>
    /// 发送此请求后，客户端应等待 <see cref="InitializeResult"/> 响应，
    /// 然后再发送 <see cref="NotificationMethods.InitializedNotification"/> 通知以完成握手。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">通信结构定义</see>。
    /// </para>
    /// </remarks>
    public sealed class InitializeRequestParams : RequestParams
    {
        /// <summary>
        /// 获取或设置客户端想要使用的模型上下文协议版本。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 协议版本使用基于日期的版本控制方案指定，格式为“YYYY-MM-DD”。
        /// 客户端和服务器必须就协议版本达成一致才能成功通信。
        /// </para>
        /// <para>
        /// 在初始化期间，服务器将检查其是否支持所请求的版本。
        /// 如果不匹配，服务器将拒绝连接并返回版本不匹配错误。
        /// </para>
        /// <para>
        /// 有关版本详情，请参阅<see href="https://spec.modelcontextprotocol.io/specification/">协议规范</see>。
        /// </para>
        /// </remarks>
        [JsonProperty("protocolVersion", Required = Required.Always)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// 获取或设置客户端的功能。
        /// </summary>
        /// <remarks>
        /// 功能定义客户端支持的功能，例如“ sampling ”或“ roots ”。
        /// </remarks>
        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }


        /// <summary>
        /// 获取或设置客户端实现的信息，包括其名称和版本。
        /// </summary>
        /// <remarks>
        /// 初始化握手期间需要此信息来识别客户端。
        /// 服务器可能会使用此信息进行日志记录、调试或兼容性检查。
        /// </remarks>
        [JsonProperty("clientInfo", Required = Required.Always)]
        public Implementation ClientInfo { get; set; }
    }
}