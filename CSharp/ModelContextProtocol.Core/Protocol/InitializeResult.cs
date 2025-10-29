using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示在建立连接期间向服务器发送 <see cref="RequestMethods.Initialize"/> 请求的结果。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="InitializeResult"/> 由服务器发送，用于响应来自客户端的 <see cref="InitializeRequestParams"/>消息。
    /// 它包含有关服务器、其功能以及将用于会话的协议版本的信息。
    /// </para>
    /// <para>
    /// 收到此响应后，客户端应发送 <see cref="NotificationMethods.InitializedNotification"/>通知以完成握手。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class InitializeResult : Result
    {
        /// <summary>
        /// 获取或设置服务器将用于此会话的模型上下文协议版本。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 这是服务器同意使用的协议版本，应与客户端
        /// 请求的版本匹配。如果不匹配，客户端应抛出异常，以防止
        /// 由于协议版本不兼容导致的通信问题。
        /// </para>
        /// <para>
        /// 该协议使用基于日期的版本控制方案，格式为“YYYY-MM-DD”。
        /// </para>
        /// <para>
        /// 有关版本详细信息，请参阅<see href="https://spec.modelcontextprotocol.io/specification/">协议规范</see>。
        /// </para>
        /// </remarks>
        [JsonProperty("protocolVersion", Required = Required.Always)]
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// 获取或设置服务器的功能。
        /// </summary>
        /// <remarks>
        /// 定义服务器支持的功能，例如“工具”、“提示”、“资源”或“日志记录”，
        /// 以及其他特定于协议的功能。
        /// </remarks>
        [JsonProperty("capabilities", Required = Required.Always)]
        public ServerCapabilities Capabilities { get; set; }

        /// <summary>
        /// 获取或设置有关服务器实现的信息，包括其名称和版本。
        /// </summary>
        /// <remarks>
        /// 此信息用于在初始化握手期间识别服务器。
        /// 客户端可以使用此信息进行日志记录、调试或兼容性检查。
        /// </remarks>
        [JsonProperty("serverInfo", Required = Required.Always)]
        public Implementation ServerInfo { get; set; }

        /// <summary>
        /// 获取或设置使用服务器及其功能的可选说明。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 这些说明为客户端提供如何有效使用服务器功能的指导。
        /// 它们可以包含有关可用工具、预期输入格式、限制的详细信息，
        /// 或任何其他有助于客户端与服务器正确交互的信息。
        /// </para>
        /// <para>
        /// 客户端应用程序通常将这些说明用作 LLM 交互的系统消息，
        /// 以提供有关可用功能的上下文。
        /// </para>
        /// </remarks>
        [JsonProperty("instructions")]
        public string Instructions { get; set; }
    }
}