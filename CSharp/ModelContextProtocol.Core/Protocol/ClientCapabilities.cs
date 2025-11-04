using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端可能支持的功能。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 功能定义了客户端在与 MCP 服务器通信时可以处理的特性和功能。
    /// 这些功能在初始化握手期间通告给服务器。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">通信结构定义</see>。
    /// </para>
    /// </remarks>
    public sealed class ClientCapabilities
    {
        /// <summary>
        /// 获取或设置客户端支持的实验性、非标准功能。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Experimental"/> 字典允许客户端声明对模型上下文协议规范中尚未标准化的功能的支持。
        /// 此扩展机制支持未来的协议增强功能，同时保持向后兼容性。
        /// </para>
        /// <para>
        /// 此字典中的值特定于实现，应在客户端和服务器实现之间进行协调。
        /// 服务器不应在未先检查的情况下假设存在任何实验性功能。
        /// </para>
        /// </remarks>
        [JsonProperty("experimental")]
        public IDictionary<string, object> Experimental { get; set; }

        /// <summary>
        /// 获取或设置客户端的根权限，根权限是资源导航的入口点。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当 <see cref="Roots"/> 为非 <see langword="null"/> 时，客户端表示它可以响应服务器请求列出根 URI 的请求。
        /// 根 URI 在协议中充当资源导航的入口点。
        /// </para>
        /// <para>
        /// 服务器可以使用 <see cref="McpServer.RequestRootsAsync"/> 向客户端请求可用根的列表，
        /// 这将触发客户端的 <see cref="McpClientHandlers.RootsHandler"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("roots")]
        public RootsCapability Roots { get; set; }

        /// <summary>
        /// 获取或设置客户端的采样能力，
        /// 该能力指示客户端是否支持代表服务器向 LLM 发出请求。
        /// </summary>
        [JsonProperty("sampling")]
        public SamplingCapability Sampling { get; set; }

        /// <summary>
        /// 获取或设置客户端的引出能力，
        /// 该能力指示客户端是否支持代表服务器从用户处引出附加信息。
        /// </summary>
        [JsonProperty("elicitation")]
        public ElicitationCapability Elicitation { get; set; }

        /// <summary>获取或设置要向客户端注册的通知处理程序。</summary>
        /// <remarks>
        /// <para>
        /// 构造后，客户端将枚举这些处理程序一次，每个通知方法键可能包含多个处理程序。
        /// 初始化后，客户端将不会重新枚举该序列。
        /// </para>
        /// <para>
        /// 通知处理程序允许客户端响应服务器发送的特定方法通知。
        /// 集合中的每个键都是一个通知方法名称，每个值都是一个回调函数，当收到包含该方法的通知时，将调用该回调函数。
        /// </para>
        /// <para>
        /// 通过 <see cref="NotificationHandlers"/> 提供的处理程序将在客户端的整个生命周期内向客户端注册。
        /// 对于瞬态处理程序，可以使用 <see cref="McpSession.RegisterNotificationHandler"/> 来注册一个处理程序，
        /// 然后可以通过处理从该方法返回的 <see cref="IAsyncDisposable"/> 来取消注册。
        /// </para>
        /// </remarks>
        [JsonIgnore]
        [Obsolete("Use McpClientOptions.Handlers.NotificationHandlers instead. This member will be removed in a subsequent release.")] // See: https://github.com/modelcontextprotocol/csharp-sdk/issues/774
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>> NotificationHandlers { get; set; }
    }
}