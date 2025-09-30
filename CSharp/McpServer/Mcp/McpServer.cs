using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Server;
using JsonRpc.Transport;
using McpServerLib.Mcp.Models;
using McpServerLib.Utils;

namespace McpServerLib.Mcp
{
    public class McpServer : IDisposable
    {
        private readonly JsonRpcServer _jsonRpcServer;
        private readonly ToolRegistry _toolRegistry;
        private readonly Dictionary<string, Resource> _resources;
        private ServerInfo _serverInfo;
        private ServerCapabilities _capabilities;
        private bool _disposed = false;

        public event EventHandler<Exception> ErrorOccurred;

        public McpServer(IJsonRpcTransport transport)
        {
            _jsonRpcServer = new JsonRpcServer(transport);
            _toolRegistry = new ToolRegistry();
            _resources = new Dictionary<string, Resource>();

            _serverInfo = new ServerInfo
            {
                Name = "C# MCP Server",
                Version = "1.0.0"
            };

            _capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = true },
                Resources = new ResourcesCapability { ListChanged = true, Subscribe = false }
            };

            RegisterMcpMethods();
            _jsonRpcServer.ErrorOccurred += OnJsonRpcError;
        }

        public void SetServerInfo(string name, string version)
        {
            _serverInfo.Name = name;
            _serverInfo.Version = version;
        }

        public void SetServerInfo(string name, string version, string title = null)
        {
            _serverInfo.Name = name;
            _serverInfo.Version = version;
            _serverInfo.Title = title;
        }

        public void RegisterToolClass<T>(T instance) where T : class
        {
            McpLogger.Debug("注册工具类实例: {0}", typeof(T).Name);
            _toolRegistry.RegisterToolClass(instance);
        }

        public void RegisterToolClass<T>() where T : class, new()
        {
            McpLogger.Debug("注册工具类类型: {0}", typeof(T).Name);
            _toolRegistry.RegisterToolClass<T>();
        }

        public void RegisterToolClass(Type type)
        {
            McpLogger.Debug("注册工具类类型: {0}", type.Name);
            _toolRegistry.RegisterToolClass(type);
        }

        public void RegisterResource(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            _resources[resource.Uri] = resource;
        }

        public int GetToolCount()
        {
            var tools = _toolRegistry.GetAllTools();
            McpLogger.Debug("GetToolCount: 当前注册了 {0} 个工具", tools?.Count ?? 0);
            return tools?.Count ?? 0;
        }

        public void UnregisterResource(string uri)
        {
            _resources.Remove(uri);
        }

        private void RegisterMcpMethods()
        {
            _jsonRpcServer.RegisterMethod<InitializeRequest>("initialize", HandleInitialize);
            _jsonRpcServer.RegisterMethod("notifications/initialized", HandleInitialized);
            _jsonRpcServer.RegisterMethod("tools/list", HandleToolsList);
            _jsonRpcServer.RegisterMethod<CallToolRequest>("tools/call", HandleToolsCall);
            _jsonRpcServer.RegisterMethod("resources/list", HandleResourcesList);
            _jsonRpcServer.RegisterMethod<ReadResourceRequest>("resources/read", HandleResourcesRead);
            _jsonRpcServer.RegisterMethod("ping", HandlePing);
        }

        private Task<object> HandleInitialize(InitializeRequest request, CancellationToken cancellationToken)
        {
            // 使用客户端请求的协议版本，如果不支持则使用默认版本
            var protocolVersion = request?.ProtocolVersion ?? "2024-11-05";

            var response = new InitializeResponse
            {
                ProtocolVersion = protocolVersion,
                Capabilities = _capabilities,
                ServerInfo = _serverInfo
            };
            return Task.FromResult<object>(response);
        }

        private Task<object> HandleInitialized(object parameters, CancellationToken cancellationToken)
        {
            McpLogger.Debug("收到 initialized 通知，MCP 服务器初始化完成");
            // 这是一个通知，不需要返回响应
            // 在这里可以进行初始化完成后的设置
            return Task.FromResult<object>(null);
        }

        private Task<object> HandleToolsList(object parameters, CancellationToken cancellationToken)
        {
            McpLogger.Debug("处理 tools/list 请求");
            var tools = _toolRegistry.GetAllTools();
            McpLogger.Debug("ToolRegistry.GetAllTools() 已调用，详细信息将在 ToolRegistry 中记录");
            var response = new ListToolsResponse { Tools = tools };
            McpLogger.Debug("返回 {0} 个工具", tools?.Count ?? 0);
            return Task.FromResult<object>(response);
        }

        private async Task<object> HandleToolsCall(CallToolRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrEmpty(request.Name))
            {
                return new CallToolResponse
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = "Tool name is required" }
                    },
                    IsError = true
                };
            }

            return await _toolRegistry.ExecuteToolAsync(request.Name, request.Arguments, cancellationToken);
        }

        private Task<object> HandleResourcesList(object parameters, CancellationToken cancellationToken)
        {
            var response = new ListResourcesResponse
            {
                Resources = _resources.Values.ToList()
            };
            return Task.FromResult<object>(response);
        }

        private Task<object> HandleResourcesRead(ReadResourceRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrEmpty(request.Uri))
            {
                throw new ArgumentException("Resource URI is required");
            }

            if (!_resources.TryGetValue(request.Uri, out var resource))
            {
                throw new ArgumentException($"Resource '{request.Uri}' not found");
            }

            // This is a basic implementation - you would typically read from actual resource
            var content = new ResourceContent
            {
                Uri = resource.Uri,
                MimeType = resource.MimeType ?? "text/plain",
                Text = $"Content of resource: {resource.Name}"
            };

            var response = new ReadResourceResponse
            {
                Contents = new List<ResourceContent> { content }
            };

            return Task.FromResult<object>(response);
        }

        private Task<object> HandlePing(object parameters, CancellationToken cancellationToken)
        {
            return Task.FromResult<object>(new { });
        }

        private void OnJsonRpcError(object sender, Exception e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _jsonRpcServer?.Dispose();
        }
    }
}