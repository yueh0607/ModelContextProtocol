using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server.Transport;
using MapleModelContextProtocol;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Server
{

    public sealed class TransportBasedMcpServer : McpServer
    {
        private readonly IMcpTransport _transport;
        private readonly McpServerOptions _options;
        private readonly Dictionary<string, RequestId> _pendingRequests;
        
        // 运行时状态
        private ClientCapabilities _clientCapabilities;
        private Implementation _clientInfo;
        private LoggingLevel? _loggingLevel;
        private bool _initialized;
        private string _negotiatedProtocolVersion;

        /// <summary>
        /// 使用指定的传输层和选项初始化新的 TransportBasedMcpServer 实例。
        /// </summary>
        /// <param name="transport">用于消息传输的传输层实现。</param>
        /// <param name="options">服务器配置选项。</param>
        public TransportBasedMcpServer(IMcpTransport transport, McpServerOptions options)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _pendingRequests = new Dictionary<string, RequestId>();
            
            _initialized = false;
            _clientCapabilities = new ClientCapabilities();
            _clientInfo = new Implementation { Name = "Unknown", Version = "0.0.0" };
        }

        /// <inheritdoc />
        public override ClientCapabilities ClientCapabilities => _clientCapabilities;

        /// <inheritdoc />
        public override Implementation ClientInfo => _clientInfo;

        /// <inheritdoc />
        public override McpServerOptions ServerOptions => _options;

        /// <inheritdoc />
        public override LoggingLevel? LoggingLevel => _loggingLevel;

        /// <inheritdoc />
        public override async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await _transport.StartAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string messageJson = await _transport.ReadMessageAsync(cancellationToken);
                    
                    if (messageJson == null)
                    {
                        // 连接已关闭
                        break;
                    }

                    // 异步处理消息，避免阻塞读取循环
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessageAsync(messageJson, cancellationToken);
                        }
                        catch (Exception)
                        {
                            // 记录错误但不中断主循环（通知不需要响应）
                            // 这里可以记录日志，但不发送响应
                        }
                    }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // 记录严重错误（连接可能已断开，无法发送响应）
                    // 这里可以记录日志，但不发送响应
                }
            }
        }

        private async Task ProcessMessageAsync(string messageJson, CancellationToken cancellationToken)
        {
            var message = McpJsonUtilities.Deserialize<JsonRpcMessage>(messageJson);

            switch (message)
            {
                case JsonRpcRequest request:
                    await HandleRequestAsync(request, cancellationToken);
                    break;

                case JsonRpcNotification notification:
                    await HandleNotificationAsync(notification, cancellationToken);
                    // 对于 HTTP 传输，通知不需要 JSON-RPC 响应，但需要发送一个空字符串来关闭 HTTP 连接
                    // 发送一个空的 JSON-RPC 成功确认（对于 HTTP 传输层）
                    await _transport.WriteMessageAsync("{}", cancellationToken);
                    break;

                case JsonRpcResponse response:
                    // 服务器收到响应（通常不应该发生，但处理它以避免错误）
                    break;

                case JsonRpcError error:
                    // 服务器收到错误（通常不应该发生）
                    break;
            }
        }

        private async Task HandleRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            Result result = null;
            JsonRpcErrorDetail errorDetail = null;

            try
            {
                switch (request.Method)
                {
                    case RequestMethods.Initialize:
                        result = await HandleInitializeAsync(request, cancellationToken);
                        break;

                    case RequestMethods.ToolsList:
                        result = await HandleToolsListAsync(request, cancellationToken);
                        break;

                    case RequestMethods.ToolsCall:
                        result = await HandleToolsCallAsync(request, cancellationToken);
                        break;

                    case RequestMethods.PromptsList:
                        result = await HandlePromptsListAsync(request, cancellationToken);
                        break;

                    case RequestMethods.PromptsGet:
                        result = await HandlePromptsGetAsync(request, cancellationToken);
                        break;

                    case RequestMethods.ResourcesList:
                        result = await HandleResourcesListAsync(request, cancellationToken);
                        break;

                    case RequestMethods.ResourcesRead:
                        result = await HandleResourcesReadAsync(request, cancellationToken);
                        break;

                    default:
                        errorDetail = new JsonRpcErrorDetail
                        {
                            Code = JsonRpcErrorCodes.MethodNotFound,
                            Message = $"Method not found: {request.Method}"
                        };
                        break;
                }
            }
            catch (Exception ex)
            {
                errorDetail = new JsonRpcErrorDetail
                {
                    Code = JsonRpcErrorCodes.InternalError,
                    Message = "Internal error",
                    Data = new { exceptionMessage = ex.Message, exceptionType = ex.GetType().Name }
                };
            }

            // JsonRpcRequest 总是需要响应（通知使用 JsonRpcNotification 类）
            JsonRpcMessage response;
            if (errorDetail != null)
            {
                response = new JsonRpcError
                {
                    Id = request.Id,
                    Error = errorDetail
                };
            }
            else
            {
                // 将 Result 对象序列化为 JObject
                var resultJson = McpJsonUtilities.Serialize(result);
                var resultJObject = JObject.Parse(resultJson);

                response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = resultJObject
                };
            }

            await SendMessageAsync(response, cancellationToken);
        }

        private Task HandleNotificationAsync(JsonRpcNotification notification, CancellationToken cancellationToken)
        {
            switch (notification.Method)
            {
                case NotificationMethods.InitializedNotification:
                    // 客户端已完成初始化
                    _initialized = true;
                    break;
                
                // 可以在这里处理其他通知
                default:
                    // 未知通知，忽略
                    break;
            }
            return Task.CompletedTask;
        }

        private Task<InitializeResult> HandleInitializeAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var initParams = ParseRequestParams<InitializeRequestParams>(request);

            // 保存客户端信息
            _clientInfo = initParams.ClientInfo;
            _clientCapabilities = initParams.Capabilities ?? new ClientCapabilities();

            // 协商协议版本
            string requestedVersion = initParams.ProtocolVersion ?? "2024-11-05";
            _negotiatedProtocolVersion = _options.ProtocolVersion ?? requestedVersion;

            // 构建服务器功能
            var capabilities = _options.Capabilities ?? new ServerCapabilities();
            if (capabilities.Tools == null && (_options.ToolCollection?.Count > 0 || _options.Handlers.ListToolsHandler != null))
            {
                capabilities.Tools = new ToolsCapability { ListChanged = false };
            }

            var result = new InitializeResult
            {
                ProtocolVersion = _negotiatedProtocolVersion,
                Capabilities = capabilities,
                ServerInfo = _options.ServerInfo ?? new Implementation { Name = "MCP Server", Version = "1.0.0" }
            };

            return Task.FromResult(result);
        }

        private async Task<ListToolsResult> HandleToolsListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var tools = new List<Tool>();

            // 从集合中添加工具
            if (_options.ToolCollection != null)
            {
                foreach (var tool in _options.ToolCollection)
                {
                    tools.Add(tool.ProtocolTool);
                }
            }

            // 如果设置了处理器，调用它并合并结果
            if (_options.Handlers.ListToolsHandler != null)
            {
                var paramsObj = ParseRequestParams<ListToolsRequestParams>(request);
                var context = new RequestContext<ListToolsRequestParams> { Params = paramsObj };
                
                var handlerResult = await _options.Handlers.ListToolsHandler(context, cancellationToken);
                if (handlerResult?.Tools != null)
                {
                    tools.AddRange(handlerResult.Tools);
                }
            }

            var result = new ListToolsResult { Tools = tools };
            return result;
        }

        private async Task<CallToolResult> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var callParams = ParseRequestParams<CallToolRequestParams>(request);

            // 首先在集合中查找工具
            var tool = _options.ToolCollection?.FirstOrDefault(t => t.ProtocolTool.Name == callParams.Name);

            if (tool != null)
            {
                // 从集合中找到工具，直接调用
                var context = new RequestContext<CallToolRequestParams> { Params = callParams };
                return await tool.InvokeAsync(context, cancellationToken);
            }

            // 如果在集合中找不到，使用处理器
            if (_options.Handlers.CallToolHandler != null)
            {
                var context = new RequestContext<CallToolRequestParams> { Params = callParams };
                return await _options.Handlers.CallToolHandler(context, cancellationToken);
            }

            // 工具未找到
            throw new InvalidOperationException($"Tool '{callParams.Name}' not found.");
        }

        private async Task<ListPromptsResult> HandlePromptsListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            var prompts = new List<Prompt>();

            // 如果设置了处理器，调用它
            if (_options.Handlers.ListPromptsHandler != null)
            {
                var paramsObj = ParseRequestParams<ListPromptsRequestParams>(request);
                var context = new RequestContext<ListPromptsRequestParams> { Params = paramsObj };
                
                var result = await _options.Handlers.ListPromptsHandler(context, cancellationToken);
                if (result?.Prompts != null)
                {
                    prompts.AddRange(result.Prompts);
                }
            }

            return new ListPromptsResult { Prompts = prompts };
        }

        private async Task<GetPromptResult> HandlePromptsGetAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_options.Handlers.GetPromptHandler == null)
            {
                throw new InvalidOperationException("GetPromptHandler is not configured.");
            }

            var paramsObj = ParseRequestParams<GetPromptRequestParams>(request);
            var context = new RequestContext<GetPromptRequestParams> { Params = paramsObj };
            
            return await _options.Handlers.GetPromptHandler(context, cancellationToken);
        }

        private async Task<ListResourcesResult> HandleResourcesListAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_options.Handlers.ListResourcesHandler == null)
            {
                return new ListResourcesResult { Resources = new List<Resource>() };
            }

            var paramsObj = ParseRequestParams<ListResourcesRequestParams>(request);
            var context = new RequestContext<ListResourcesRequestParams> { Params = paramsObj };
            
            // McpRequestHandler 返回 ValueTask，可以直接 await
            return await _options.Handlers.ListResourcesHandler(context, cancellationToken);
        }

        private async Task<ReadResourceResult> HandleResourcesReadAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            if (_options.Handlers.ReadResourceHandler == null)
            {
                throw new InvalidOperationException("ReadResourceHandler is not configured.");
            }

            var paramsObj = ParseRequestParams<ReadResourceRequestParams>(request);
            var context = new RequestContext<ReadResourceRequestParams> { Params = paramsObj };
            
            return await _options.Handlers.ReadResourceHandler(context, cancellationToken);
        }

        private TParams ParseRequestParams<TParams>(JsonRpcRequest request) where TParams : RequestParams
        {
            if (request.Params == null)
            {
                return Activator.CreateInstance<TParams>();
            }

            // 将 JToken 转换为强类型参数
            return request.Params.ToObject<TParams>(Newtonsoft.Json.JsonSerializer.Create(McpJsonUtilities.DefaultSettings));
        }

        private async Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        {
            string json = McpJsonUtilities.Serialize(message);
            await _transport.WriteMessageAsync(json, cancellationToken);
        }

        private async Task SendErrorResponseAsync(RequestId? requestId, int code, string message, CancellationToken cancellationToken)
        {
            if (requestId == null)
                return; // 通知不需要响应

            var error = new JsonRpcError
            {
                Id = requestId.Value,
                Error = new JsonRpcErrorDetail
                {
                    Code = code,
                    Message = message
                }
            };

            await SendMessageAsync(error, cancellationToken);
        }
    }
}

