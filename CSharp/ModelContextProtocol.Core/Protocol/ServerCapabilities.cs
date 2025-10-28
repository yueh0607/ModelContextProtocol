namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器可能支持的功能。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 服务器功能定义了客户端连接时可用的特性和功能。
    /// 这些功能在初始化握手期间向客户端公布。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class ServerCapabilities
    {
        // /// <summary>
        // /// 获取或设置服务器支持的实验性、非标准功能。
        // /// </summary>
        // /// <remarks>
        // /// <para>
        // /// <see cref="Experimental"/> 字典允许服务器通告对模型上下文协议规范中尚未标准化的功能的支持。
        // /// 此扩展机制支持未来协议的增强，同时保持向后兼容性。
        // /// </para>
        // /// <para>
        // /// 此字典中的值特定于实现，应在客户端和服务器实现之间进行协调。
        // /// 客户端不应在未先检查的情况下假设存在任何实验性功能。
        // /// </para>
        // /// </remarks>
        // [JsonProperty("experimental")]
        // public IDictionary<string, object> Experimental { get; set; }
        //
        // /// <summary>
        // /// 获取或设置服务器的日志功能，支持向客户端发送日志消息。
        // /// </summary>
        // [JsonProperty("logging")]
        // public LoggingCapability? Logging { get; set; }
        //
        // /// <summary>
        // /// 获取或设置服务器的提示功能，用于提供客户端可以发现和使用的预定义提示模板。
        // /// </summary>
        // [JsonPropertyName("prompts")]
        // public PromptsCapability? Prompts { get; set; }
        //
        // /// <summary>
        // /// 获取或设置服务器的资源能力，用于提供客户端可以发现和使用的预定义资源。
        // /// </summary>
        // [JsonPropertyName("resources")]
        // public ResourcesCapability? Resources { get; set; }
        //
        // /// <summary>
        // /// 获取或设置服务器的工具功能，用于列出客户端能够调用的工具。
        // /// </summary>
        // [JsonPropertyName("tools")]
        // public ToolsCapability? Tools { get; set; }
        //
        // /// <summary>
        // /// 获取或设置服务器的补全功能，以支持参数自动补全建议。
        // /// </summary>
        // [JsonPropertyName("completions")]
        // public CompletionsCapability? Completions { get; set; }
        //
        // /// <summary>获取或设置要向服务器注册的通知处理程序。</summary>
        // /// <remarks>
        // /// <para>
        // /// 构造后，服务器将枚举这些处理程序一次，每个通知方法键可能包含多个处理程序。
        // /// 初始化后，服务器将不会重新枚举该序列。
        // /// </para>
        // /// <para>
        // /// 通知处理程序允许服务器响应客户端发送的特定方法通知。
        // /// 集合中的每个键都是一个通知方法名称，每个值都是一个回调函数，当收到包含该方法的通知时，将调用该回调函数。
        // /// </para>
        // /// <para>
        // /// 通过 <see cref="NotificationHandlers"/> 提供的处理程序将在服务器的整个生命周期内向服务器注册。
        // /// 对于瞬态处理程序，可以使用 <see cref="McpSession.RegisterNotificationHandler"/> 来注册一个处理程序，
        // /// 然后可以通过处理从该方法返回的 <see cref="IAsyncDisposable"/> 来取消注册。
        // /// </para>
        // /// </remarks>
        // [JsonIgnore]
        // [Obsolete($"Use {nameof(McpServerOptions.Handlers.NotificationHandlers)} instead. This member will be removed in a subsequent release.")] // See: https://github.com/modelcontextprotocol/csharp-sdk/issues/774
        // [EditorBrowsable(EditorBrowsableState.Never)]
        // public IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>>? NotificationHandlers { get; set; }
    }
}