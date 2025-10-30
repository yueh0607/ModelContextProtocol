using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP
{
    /// <summary>
    /// MCP工具的抽象基类
    /// </summary>
    public abstract class McpTool
    {
        public abstract string Name { get; }
        public abstract string Description { get; }

        /// <summary>
        /// 执行工具调用
        /// </summary>
        public abstract Task<CallToolResult> ExecuteAsync(Dictionary<string, object> arguments);

        /// <summary>
        /// 生成输入的JSON Schema
        /// </summary>
        public virtual JObject GetInputSchema()
        {
            return new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject(),
                ["required"] = new JArray()
            };
        }

        /// <summary>
        /// 创建文本响应
        /// </summary>
        protected CallToolResult CreateTextResult(string text, bool isError = false)
        {
            return new CallToolResult
            {
                Content = new List<Content>
                {
                    new Content
                    {
                        Type = "text",
                        Text = text
                    }
                },
                IsError = isError ? (bool?)true : null
            };
        }
    }

    /// <summary>
    /// MCP资源的抽象基类
    /// </summary>
    public abstract class McpResource
    {
        public abstract string Uri { get; }
        public abstract string Name { get; }
        public virtual string Description { get; }
        public virtual string MimeType { get; }

        /// <summary>
        /// 读取资源内容
        /// </summary>
        public abstract Task<ResourceContents> ReadAsync();
    }

    /// <summary>
    /// MCP Prompt的抽象基类
    /// </summary>
    public abstract class McpPrompt
    {
        public abstract string Name { get; }
        public virtual string Description { get; }
        public virtual List<PromptArgument> Arguments { get; }

        /// <summary>
        /// 获取prompt内容
        /// </summary>
        public abstract Task<GetPromptResult> GetAsync(Dictionary<string, string> arguments);
    }

    /// <summary>
    /// MCP服务器核心实现
    /// </summary>
    public class McpServer
    {
        private readonly Dictionary<string, McpTool> _tools = new Dictionary<string, McpTool>();
        private readonly Dictionary<string, McpResource> _resources = new Dictionary<string, McpResource>();
        private readonly Dictionary<string, McpPrompt> _prompts = new Dictionary<string, McpPrompt>();
        private bool _initialized = false;
        private ClientCapabilities _clientCapabilities;

        public string ServerName { get; set; } = "Unity MCP Server";
        public string ServerVersion { get; set; } = "1.0.0";

        // 事件：用于日志输出
        public event Action<string> OnLog;

        /// <summary>
        /// 注册工具
        /// </summary>
        public void RegisterTool(McpTool tool)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            _tools[tool.Name] = tool;
            Log($"Registered tool: {tool.Name}");
        }

        /// <summary>
        /// 注册资源
        /// </summary>
        public void RegisterResource(McpResource resource)
        {
            if (resource == null) throw new ArgumentNullException(nameof(resource));
            _resources[resource.Uri] = resource;
            Log($"Registered resource: {resource.Uri}");
        }

        /// <summary>
        /// 注册Prompt
        /// </summary>
        public void RegisterPrompt(McpPrompt prompt)
        {
            if (prompt == null) throw new ArgumentNullException(nameof(prompt));
            _prompts[prompt.Name] = prompt;
            Log($"Registered prompt: {prompt.Name}");
        }

        /// <summary>
        /// 处理JSON-RPC请求
        /// </summary>
        public async Task<string> HandleRequestAsync(string jsonRequest)
        {
            try
            {
                // 解析请求
                var request = JsonConvert.DeserializeObject<JsonRpcRequest>(jsonRequest);

                if (request == null || request.JsonRpc != "2.0")
                {
                    return CreateErrorResponse(null, JsonRpcErrorCodes.InvalidRequest, "Invalid JSON-RPC request");
                }

                Log($"Handling method: {request.Method}");

                // 路由到相应的处理方法
                object result = null;
                switch (request.Method)
                {
                    case "initialize":
                        result = await HandleInitializeAsync(request.Params);
                        break;
                    case "tools/list":
                        result = HandleListTools();
                        break;
                    case "tools/call":
                        result = await HandleCallToolAsync(request.Params);
                        break;
                    case "resources/list":
                        result = HandleListResources();
                        break;
                    case "resources/read":
                        result = await HandleReadResourceAsync(request.Params);
                        break;
                    case "prompts/list":
                        result = HandleListPrompts();
                        break;
                    case "prompts/get":
                        result = await HandleGetPromptAsync(request.Params);
                        break;
                    default:
                        return CreateErrorResponse(request.Id, JsonRpcErrorCodes.MethodNotFound,
                            $"Method not found: {request.Method}");
                }

                // 返回成功响应
                var response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (JsonException ex)
            {
                Log($"Parse error: {ex.Message}");
                return CreateErrorResponse(null, JsonRpcErrorCodes.ParseError, "Parse error", ex.Message);
            }
            catch (Exception ex)
            {
                Log($"Internal error: {ex.Message}");
                return CreateErrorResponse(null, JsonRpcErrorCodes.InternalError, "Internal error", ex.Message);
            }
        }

        private async Task<InitializeResult> HandleInitializeAsync(JObject paramsObj)
        {
            var initParams = paramsObj.ToObject<InitializeParams>();
            _clientCapabilities = initParams.Capabilities;
            _initialized = true;

            Log($"Initialized with client: {initParams.ClientInfo?.Name} {initParams.ClientInfo?.Version}");

            return new InitializeResult
            {
                ProtocolVersion = "2024-11-05",
                ServerInfo = new Implementation
                {
                    Name = ServerName,
                    Version = ServerVersion
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = _tools.Count > 0 ? new ToolsCapability() : null,
                    Resources = _resources.Count > 0 ? new ResourcesCapability() : null,
                    Prompts = _prompts.Count > 0 ? new PromptsCapability() : null
                }
            };
        }

        private ListToolsResult HandleListTools()
        {
            var result = new ListToolsResult();

            foreach (var tool in _tools.Values)
            {
                result.Tools.Add(new Tool
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    InputSchema = tool.GetInputSchema()
                });
            }

            return result;
        }

        private async Task<CallToolResult> HandleCallToolAsync(JObject paramsObj)
        {
            var callParams = paramsObj.ToObject<CallToolParams>();

            if (!_tools.TryGetValue(callParams.Name, out var tool))
            {
                return new CallToolResult
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = $"Tool not found: {callParams.Name}" }
                    },
                    IsError = true
                };
            }

            try
            {
                return await tool.ExecuteAsync(callParams.Arguments);
            }
            catch (Exception ex)
            {
                Log($"Tool execution error: {ex.Message}");
                return new CallToolResult
                {
                    Content = new List<Content>
                    {
                        new Content { Type = "text", Text = $"Tool execution failed: {ex.Message}" }
                    },
                    IsError = true
                };
            }
        }

        private ListResourcesResult HandleListResources()
        {
            var result = new ListResourcesResult();

            foreach (var resource in _resources.Values)
            {
                result.Resources.Add(new Resource
                {
                    Uri = resource.Uri,
                    Name = resource.Name,
                    Description = resource.Description,
                    MimeType = resource.MimeType
                });
            }

            return result;
        }

        private async Task<ReadResourceResult> HandleReadResourceAsync(JObject paramsObj)
        {
            var readParams = paramsObj.ToObject<ReadResourceParams>();

            if (!_resources.TryGetValue(readParams.Uri, out var resource))
            {
                throw new Exception($"Resource not found: {readParams.Uri}");
            }

            var contents = await resource.ReadAsync();
            return new ReadResourceResult
            {
                Contents = new List<ResourceContents> { contents }
            };
        }

        private ListPromptsResult HandleListPrompts()
        {
            var result = new ListPromptsResult();

            foreach (var prompt in _prompts.Values)
            {
                result.Prompts.Add(new Prompt
                {
                    Name = prompt.Name,
                    Description = prompt.Description,
                    Arguments = prompt.Arguments
                });
            }

            return result;
        }

        private async Task<GetPromptResult> HandleGetPromptAsync(JObject paramsObj)
        {
            var getParams = paramsObj.ToObject<GetPromptParams>();

            if (!_prompts.TryGetValue(getParams.Name, out var prompt))
            {
                throw new Exception($"Prompt not found: {getParams.Name}");
            }

            return await prompt.GetAsync(getParams.Arguments);
        }

        private string CreateErrorResponse(object id, int code, string message, object data = null)
        {
            var response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message,
                    Data = data
                }
            };

            return JsonConvert.SerializeObject(response);
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[McpServer] {message}");
        }
    }
}
