using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Client;
using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示在模型上下文协议 (MCP) 中启用根资源发现的客户端功能。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 如果出现在 <see cref="ClientCapabilities"/> 中，
    /// 则表示客户端支持列出作为资源导航入口点的根 URI。
    /// </para>
    /// <para>
    /// 根功能为服务器建立了一种机制，用于发现和访问客户端提供的资源的分层结构。
    /// 根 URI 表示顶级入口点，服务器可以从该入口点导航到特定资源。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class RootsCapability
    {
        /// <summary>
        /// 获取或设置客户端是否支持根列表更改通知。
        /// </summary>
        /// <remarks>
        /// 设置为 <see langword="true"/> 时，客户端可以在添加、
        /// 删除或修改根时通知服务器，从而允许服务器相应地刷新其根缓存。
        /// 这使服务器能够与客户端可用根的更改保持同步。
        /// </remarks>
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; }

        /// <summary>
        /// 获取或设置 <see cref="RequestMethods.RootsList"/> 请求的处理程序。
        /// </summary>
        /// <remarks>
        /// 当客户端发送 <see cref="RequestMethods.RootsList"/> 请求以检索可用根时，将调用此处理程序。
        /// 该处理程序接收请求参数，并应返回包含可用根集合的 <see cref="ListRootsResult"/>。
        /// </remarks>
        [JsonIgnore]
        [Obsolete("Use McpClientOptions.Handlers.RootsHandler instead. This member will be removed in a subsequent release.")] // See: https://github.com/modelcontextprotocol/csharp-sdk/issues/774
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<ListRootsRequestParams, CancellationToken, ValueTask<ListRootsResult>> RootsHandler { get; set; }
    }
}