using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

using ModelContextProtocol.Json;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端在交互过程中提供服务器请求的附加信息的能力。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此功能使 MCP 客户端能够响应来自 MCP 服务器的启发式请求。
    /// </para>
    /// <para>
    /// 启用此功能后，MCP 服务器可以请求客户端在交互过程中提供附加信息。
    /// 客户端必须设置 <see cref="McpClientHandlers.ElicitationHandler"/> 来处理这些请求。
    /// </para>
    /// <para>
    /// 此类特意留空，因为模型上下文协议规范目前尚未为采样功能定义其他属性。
    /// 该规范的未来版本可能会通过其他配置选项扩展此功能。
    /// </para>
    /// </remarks>
    public sealed class ElicitationCapability
    {
        /// <summary>
        /// 获取或设置用于处理 <see cref="RequestMethods.ElicitationCreate"/> 请求的处理程序。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当 MCP 服务器请求客户端在交互过程中提供额外信息时，将调用此处理程序函数。
        /// </para>
        /// <para>
        /// 该处理程序接收消息参数和取消令牌。
        /// 它应该返回一个包含对引出请求的响应的 <see cref="ElicitResult"/>。
        /// </para>
        /// </remarks>
        [JsonIgnore]
        [Obsolete("Use McpClientOptions.Handlers.ElicitationHandler instead. This member will be removed in a subsequent release.")] // See: https://github.com/modelcontextprotocol/csharp-sdk/issues/774
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<ElicitRequestParams, CancellationToken, Task<ElicitResult>> ElicitationHandler { get; set; }
    }
}