using System.Collections.Generic;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// 提供一个上下文容器，用于访问客户端请求参数和请求的资源。
    /// </summary>
    /// <typeparam name="TParams">每个 MCP 操作特定的请求参数类型。</typeparam>
    /// <remarks>
    /// <see cref="RequestContext{TParams}"/> 封装了处理 MCP 请求的所有上下文信息。
    /// 此类型通常作为在 IMcpServerBuilder 中注册的处理程序委托中的参数接收，
    /// 也可以作为参数注入到 <see cref="McpServerTool"/> 中。
    /// </remarks>
    public sealed class RequestContext<TParams>
    {
        public TParams Params { get; set; }

        public IDictionary<string, object> Items { get; set; }
    }
}