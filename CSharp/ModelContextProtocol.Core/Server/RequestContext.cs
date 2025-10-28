namespace ModelContextProtocol.Server
{
    /// <summary>
    /// 提供一个上下文容器，用于访问客户端请求参数和请求的资源。
    /// TODO: 整个类还没有实现，涉及到完整的 McpServer 体系实现
    /// </summary>
    /// <typeparam name="TParams">每个 MCP 操作特定的请求参数类型。</typeparam>
    /// <remarks>
    /// <see cref="RequestContext{TParams}"/> 封装了处理 MCP 请求的所有上下文信息。
    /// 此类型通常作为在 IMcpServerBuilder 中注册的处理程序委托中的参数接收，
    /// 也可以作为参数注入到 <see cref="McpServerTool"/> 中。
    /// </remarks>
    public sealed class RequestContext<TParams>
    {
        // // <summary>与此实例关联的服务器。</summary>
        // private McpServer _server;
        //
        // private IDictionary<string, object> _items;
        //
        // /// <summary>
        // /// Initializes a new instance of the <see cref="RequestContext{TParams}"/> class with the specified server and JSON-RPC request.
        // /// </summary>
        // /// <param name="server">The server with which this instance is associated.</param>
        // /// <param name="jsonRpcRequest">The JSON-RPC request associated with this context.</param>
        // public RequestContext(McpServer server, JsonRpcRequest jsonRpcRequest)
        // {
        //     if (server == null)
        //         throw new ArgumentNullException(nameof(server));
        //     if (jsonRpcRequest == null)
        //         throw new ArgumentNullException(nameof(jsonRpcRequest));
        //
        //     _server = server;
        //     JsonRpcRequest = jsonRpcRequest;
        //     Services = server.Services;
        //     if (jsonRpcRequest.Context != null)
        //         User = jsonRpcRequest.Context.User;
        // }
        //
        // /// <summary>Gets or sets the server with which this instance is associated.</summary>
        // public McpServer Server
        // {
        //     get { return _server; }
        //     set
        //     {
        //         if (value == null)
        //             throw new ArgumentNullException(nameof(value));
        //         _server = value;
        //     }
        // }
        //
        // /// <summary>
        // /// Gets or sets a key/value collection that can be used to share data within the scope of this request.
        // /// </summary>
        // public IDictionary<string, object> Items
        // {
        //     get
        //     {
        //         if (_items == null)
        //             _items = new Dictionary<string, object>();
        //         return _items;
        //     }
        //     set { _items = value; }
        // }
        //
        // /// <summary>Gets or sets the services associated with this request.</summary>
        // /// <remarks>
        // /// This may not be the same instance stored in <see cref="McpServer.Services"/>
        // /// if <see cref="McpServerOptions.ScopeRequests"/> was true, in which case this
        // /// might be a scoped <see cref="IServiceProvider"/> derived from the server's
        // /// <see cref="McpServer.Services"/>.
        // /// </remarks>
        // public IServiceProvider Services { get; set; }
        //
        // /// <summary>Gets or sets the user associated with this request.</summary>
        // public ClaimsPrincipal User { get; set; }
        //
        // /// <summary>Gets or sets the parameters associated with this request.</summary>
        // public TParams Params { get; set; }
        //
        // /// <summary>
        // /// Gets or sets the primitive that matched the request.
        // /// </summary>
        // public IMcpServerPrimitive MatchedPrimitive { get; set; }
        //
        // /// <summary>
        // /// Gets the JSON-RPC request associated with this context.
        // /// </summary>
        // /// <remarks>
        // /// This property provides access to the complete JSON-RPC request that initiated this handler invocation,
        // /// including the method name, parameters, request ID, and associated transport and user information.
        // /// </remarks>
        // public JsonRpcRequest JsonRpcRequest { get; private set; }
    }
}