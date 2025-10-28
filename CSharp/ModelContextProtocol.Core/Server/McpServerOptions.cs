namespace ModelContextProtocol.Server
{
    // TODO: 整个类还没有实现，涉及到完整的 McpServer 体系实现
    public sealed class McpServerOptions
    {
        private McpServerHandlers _handlers;

        // /// <summary>
        // /// 获取或设置此服务器实现的相关信息，包括其名称和版本。
        // /// </summary>
        // /// <remarks>
        // /// 此信息在初始化期间发送给客户端，用于识别服务器。
        // /// 它会显示在客户端日志中，并可用于调试和兼容性检查。
        // /// </remarks>
        // public Implementation ServerInfo { get; set; }
        //
        // /// <summary>
        // /// 获取或设置要通告给客户端的服务器功能。
        // /// </summary>
        // /// <remarks>
        // /// 这些决定了客户端连接时可用的功能。
        // /// 功能可以包括“工具”、“提示”、“资源”、“日志记录”以及其他特定于协议的功能。
        // /// </remarks>
        // public ServerCapabilities Capabilities { get; set; }
        //
        // /// <summary>
        // /// 使用基于日期的版本控制方案获取或设置此服务器支持的协议版本。
        // /// </summary>
        // /// <remarks>
        // /// 协议版本定义了此服务器支持的功能和消息格式。
        // /// 使用基于日期的版本控制方案，格式为“YYYY-MM-DD”。
        // /// 如果 <see langword="null"/>，则服务器将向客户端通告客户端请求的版本，
        // /// 如果已知该版本受支持，则服务器将通告客户端请求的版本，否则将通告服务器支持的最新版本。
        // /// </remarks>
        // public string ProtocolVersion { get; set; }
        //
        // /// <summary>
        // /// Gets or sets a timeout used for the client-server initialization handshake sequence.
        // /// </summary>
        // /// <remarks>
        // /// This timeout determines how long the server will wait for client responses during
        // /// the initialization protocol handshake. If the client doesn't respond within this timeframe,
        // /// the initialization process will be aborted.
        // /// </remarks>
        // public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(60);
        //
        // /// <summary>
        // /// Gets or sets optional server instructions to send to clients.
        // /// </summary>
        // /// <remarks>
        // /// These instructions are sent to clients during the initialization handshake and provide
        // /// guidance on how to effectively use the server's capabilities. They can include details
        // /// about available tools, expected input formats, limitations, or other helpful information.
        // /// Client applications typically use these instructions as system messages for LLM interactions
        // /// to provide context about available functionality.
        // /// </remarks>
        // public string? ServerInstructions { get; set; }
        //
        // /// <summary>
        // /// Gets or sets whether to create a new service provider scope for each handled request.
        // /// </summary>
        // /// <remarks>
        // /// The default is <see langword="true"/>. When <see langword="true"/>, each invocation of a request
        // /// handler will be invoked within a new service scope.
        // /// </remarks>
        // public bool ScopeRequests { get; set; } = true;
        //
        // /// <summary>
        // /// Gets or sets preexisting knowledge about the client including its name and version to help support
        // /// stateless Streamable HTTP servers that encode this knowledge in the mcp-session-id header.
        // /// </summary>
        // /// <remarks>
        // /// <para>
        // /// When not specified, this information is sourced from the client's initialize request.
        // /// </para>
        // /// </remarks>
        // public Implementation? KnownClientInfo { get; set; }
        //
        // /// <summary>
        // /// Gets the filter collections for MCP server handlers.
        // /// </summary>
        // /// <remarks>
        // /// This property provides access to filter collections that can be used to modify the behavior 
        // /// of various MCP server handlers. Filters are applied in reverse order, so the last filter 
        // /// added will be the outermost (first to execute).
        // /// </remarks>
        // public McpServerFilters Filters { get; } = new();
        //
        // /// <summary>
        // /// Gets or sets the container of handlers used by the server for processing protocol messages.
        // /// </summary>
        // public McpServerHandlers Handlers 
        // { 
        //     get => _handlers ??= new();
        //     set
        //     { 
        //         Throw.IfNull(value); 
        //         _handlers = value;
        //     }
        // }
        //
        // /// <summary>
        // /// Gets or sets a collection of tools served by the server.
        // /// </summary>
        // /// <remarks>
        // /// Tools specified via <see cref="ToolCollection"/> augment the <see cref="McpServerHandlers.ListToolsHandler"/> and
        // /// <see cref="McpServerHandlers.CallToolHandler"/>, if provided. ListTools requests will output information about every tool
        // /// in <see cref="ToolCollection"/> and then also any tools output by <see cref="McpServerHandlers.ListToolsHandler"/>, if it's
        // /// non-<see langword="null"/>. CallTool requests will first check <see cref="ToolCollection"/> for the tool
        // /// being requested, and if the tool is not found in the <see cref="ToolCollection"/>, any specified <see cref="McpServerHandlers.CallToolHandler"/>
        // /// will be invoked as a fallback.
        // /// </remarks>
        // public McpServerPrimitiveCollection<McpServerTool>? ToolCollection { get; set; }
        //
        // /// <summary>
        // /// Gets or sets a collection of resources served by the server.
        // /// </summary>
        // /// <remarks>
        // /// <para>
        // /// Resources specified via <see cref="ResourceCollection"/> augment the <see cref="McpServerHandlers.ListResourcesHandler"/>, <see cref="McpServerHandlers.ListResourceTemplatesHandler"/>
        // /// and <see cref="McpServerHandlers.ReadResourceHandler"/> handlers, if provided. Resources with template expressions in their URI templates are considered resource templates
        // /// and are listed via ListResourceTemplate, whereas resources without template parameters are considered static resources and are listed with ListResources.
        // /// </para>
        // /// <para>
        // /// ReadResource requests will first check the <see cref="ResourceCollection"/> for the exact resource being requested. If no match is found, they'll proceed to
        // /// try to match the resource against each resource template in <see cref="ResourceCollection"/>. If no match is still found, the request will fall back to
        // /// any handler registered for <see cref="McpServerHandlers.ReadResourceHandler"/>.
        // /// </para>
        // /// </remarks>
        // public McpServerResourceCollection? ResourceCollection { get; set; }
        //
        // /// <summary>
        // /// Gets or sets a collection of prompts that will be served by the server.
        // /// </summary>
        // /// <remarks>
        // /// <para>
        // /// The <see cref="PromptCollection"/> contains the predefined prompts that clients can request from the server.
        // /// This collection works in conjunction with <see cref="McpServerHandlers.ListPromptsHandler"/> and <see cref="McpServerHandlers.GetPromptHandler"/>
        // /// when those are provided:
        // /// </para>
        // /// <para>
        // /// - For <see cref="RequestMethods.PromptsList"/> requests: The server returns all prompts from this collection 
        // ///   plus any additional prompts provided by the <see cref="McpServerHandlers.ListPromptsHandler"/> if it's set.
        // /// </para>
        // /// <para>
        // /// - For <see cref="RequestMethods.PromptsGet"/> requests: The server first checks this collection for the requested prompt.
        // ///   If not found, it will invoke the <see cref="McpServerHandlers.GetPromptHandler"/> as a fallback if one is set.
        // /// </para>
        // /// </remarks>
        // public McpServerPrimitiveCollection<McpServerPrompt>? PromptCollection { get; set; }
    }
}