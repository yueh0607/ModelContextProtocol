using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Server;
using JsonRpc.Transport;
using McpServerLib.Mcp.Models;

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

        public void RegisterToolClass<T>(T instance) where T : class
        {
            _toolRegistry.RegisterToolClass(instance);
        }

        public void RegisterToolClass<T>() where T : class, new()
        {
            _toolRegistry.RegisterToolClass<T>();
        }

        public void RegisterToolClass(Type type)
        {
            _toolRegistry.RegisterToolClass(type);
        }

        public void RegisterResource(Resource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            _resources[resource.Uri] = resource;
        }

        public void UnregisterResource(string uri)
        {
            _resources.Remove(uri);
        }

        private void RegisterMcpMethods()
        {
            _jsonRpcServer.RegisterMethod<InitializeRequest>("initialize", HandleInitialize);
            _jsonRpcServer.RegisterMethod("tools/list", HandleToolsList);
            _jsonRpcServer.RegisterMethod<CallToolRequest>("tools/call", HandleToolsCall);
            _jsonRpcServer.RegisterMethod("resources/list", HandleResourcesList);
            _jsonRpcServer.RegisterMethod<ReadResourceRequest>("resources/read", HandleResourcesRead);
            _jsonRpcServer.RegisterMethod("ping", HandlePing);
        }

        private Task<object> HandleInitialize(InitializeRequest request, CancellationToken cancellationToken)
        {
            var response = new InitializeResponse
            {
                ProtocolVersion = "2024-11-05",
                Capabilities = _capabilities,
                ServerInfo = _serverInfo
            };
            return Task.FromResult<object>(response);
        }

        private Task<object> HandleToolsList(object parameters, CancellationToken cancellationToken)
        {
            var tools = _toolRegistry.GetAllTools();
            var response = new ListToolsResponse { Tools = tools };
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