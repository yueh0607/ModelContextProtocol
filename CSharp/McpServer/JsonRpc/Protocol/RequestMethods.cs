namespace JsonRpc.Protocol
{

    /// <summary>
    /// 提供 MCP 协议中常用请求方法名称的常量。
    /// </summary>
    public static class RequestMethods
    {
        /// <summary>
        /// 客户端发送的请求方法的名称，用于请求服务器工具列表。
        /// </summary>
        public const string ToolsList = "tools/list";

        /// <summary>
        /// 客户端发送的请求方法名称，用于请求服务器调用特定工具。
        /// </summary>
        public const string ToolsCall = "tools/call";

        /// <summary>
        /// 客户端发送的请求方法的名称，用于请求服务器提示列表。
        /// </summary>
        public const string PromptsList = "prompts/list";

        /// <summary>
        /// 客户端发送的请求方法的名称，用于获取服务器提供的提示。
        /// </summary>
        public const string PromptsGet = "prompts/get";

        /// <summary>
        /// 客户端发送的请求方法名称，用于请求服务器资源列表。
        /// </summary>
        public const string ResourcesList = "resources/list";

        /// <summary>
        /// 客户端发送的用于读取特定服务器资源的请求方法的名称。
        /// </summary>
        public const string ResourcesRead = "resources/read";

        /// <summary>
        /// 客户端发送的请求方法名称，用于请求服务器的资源模板列表。
        /// </summary>
        public const string ResourcesTemplatesList = "resources/templates/list";

        /// <summary>
        /// 客户端发送的请求方法名称，用于请求 <see cref="NotificationMethods.ResourceUpdatedNotification"/>
        /// 每当特定资源发生变化时，服务器都会发出通知。
        /// </summary>
        public const string ResourcesSubscribe = "resources/subscribe";

        /// <summary>
        /// 客户端发送的用于取消订阅服务器通知的请求方法的名称 <see cref="NotificationMethods.ResourceUpdatedNotification"/>
        /// 来自服务器的通知。
        /// </summary>
        public const string ResourcesUnsubscribe = "resources/unsubscribe";

        /// <summary>
        /// 服务器发送的请求方法的名称，用于请求客户端根列表。
        /// </summary>
        public const string RootsList = "roots/list";

        /// <summary>
        /// 任一端点发送的请求方法的名称，用于检查连接的端点是否仍然处于活动状态。
        /// </summary>
        public const string Ping = "ping";

        /// <summary>
        /// 客户端向服务器发送的用于调整日志级别的请求方法的名称。
        /// </summary>
        /// <remarks>
        /// 此请求允许客户端通过设置最低严重级别阈值来控制从服务器接收的日志消息。
        /// 处理此请求后，服务器将把严重级别等于或高于指定级别的日志消息作为
        /// <see cref="NotificationMethods.LoggingMessageNotification"/> 通知发送给客户端。
        /// </remarks>
        public const string LoggingSetLevel = "logging/setLevel";

        /// <summary>
        /// 客户端向服务器发送请求方法的名称，用于请求补全建议。
        /// </summary>
        /// <remarks>
        /// 用于为资源引用或提示模板中的参数提供类似自动补全的功能。
        /// 客户端提供引用（资源或提示）、参数名称和部分值，服务器响应匹配的补全选项。
        /// </remarks>
        public const string CompletionComplete = "completion/complete";

        /// <summary>
        /// 服务器发送的请求方法的名称，用于通过客户端对大型语言模型 (LLM) 进行采样。
        /// </summary>
        /// <remarks>
        /// 此请求允许服务器利用客户端可用的 LLM 根据提供的消息生成文本或图像响应。
        /// 它是模型上下文协议 (MCP) 中采样功能的一部分，使服务器能够访问客户端 AI 模型，而无需直接通过 API 访问这些模型。
        /// </remarks>
        public const string SamplingCreateMessage = "sampling/createMessage";

        /// <summary>
        /// 客户端向服务器发送的请求方法的名称，用于通过客户端获取用户的更多信息。
        /// </summary>
        /// <remarks>
        /// 当服务器需要从客户端获取更多信息以继续执行任务或交互时，使用此请求。
        /// 服务器可以向用户请求结构化数据，并使用可选的 JSON 模式来验证响应。
        /// </remarks>
        public const string ElicitationCreate = "elicitation/create";

        /// <summary>
        /// 客户端首次连接服务器时，向服务器发送的请求方法名称，用于初始化服务器。
        /// </summary>
        /// <remarks>
        /// 初始化请求是客户端向服务器发送的第一个请求。
        /// 它在连接建立期间向服务器提供客户端信息和功能。
        /// 服务器会响应其自身的功能和信息，以建立会话的协议版本和可用功能。
        /// </remarks>
        public const string Initialize = "initialize";
    }
}