using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityMCP
{
    // ==================== JSON-RPC 2.0 基础类型 ====================

    [Serializable]
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public JObject Params { get; set; }
    }

    [Serializable]
    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }
    }

    [Serializable]
    public class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }
    }

    [Serializable]
    public class JsonRpcNotification
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }
    }

    // ==================== MCP 初始化 ====================

    [Serializable]
    public class InitializeParams
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public Implementation ClientInfo { get; set; }
    }

    [Serializable]
    public class Implementation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }
    }

    [Serializable]
    public class ClientCapabilities
    {
        [JsonProperty("experimental", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Experimental { get; set; }

        [JsonProperty("sampling", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Sampling { get; set; }

        [JsonProperty("roots", NullValueHandling = NullValueHandling.Ignore)]
        public RootsCapability Roots { get; set; }
    }

    [Serializable]
    public class RootsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    [Serializable]
    public class InitializeResult
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; } = "2024-11-05";

        [JsonProperty("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonProperty("serverInfo")]
        public Implementation ServerInfo { get; set; }
    }

    [Serializable]
    public class ServerCapabilities
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public ToolsCapability Tools { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public ResourcesCapability Resources { get; set; }

        [JsonProperty("prompts", NullValueHandling = NullValueHandling.Ignore)]
        public PromptsCapability Prompts { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Logging { get; set; }
    }

    [Serializable]
    public class ToolsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    [Serializable]
    public class ResourcesCapability
    {
        [JsonProperty("subscribe", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Subscribe { get; set; }

        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    [Serializable]
    public class PromptsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    // ==================== Tools ====================

    [Serializable]
    public class ListToolsResult
    {
        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; } = new List<Tool>();
    }

    [Serializable]
    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }

    [Serializable]
    public class CallToolParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public Dictionary<string, object> Arguments { get; set; }
    }

    [Serializable]
    public class CallToolResult
    {
        [JsonProperty("content")]
        public List<Content> Content { get; set; } = new List<Content>();

        [JsonProperty("isError", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsError { get; set; }
    }

    [Serializable]
    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; } // "text", "image", "resource"

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public string Data { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }
    }

    // ==================== Resources ====================

    [Serializable]
    public class ListResourcesResult
    {
        [JsonProperty("resources")]
        public List<Resource> Resources { get; set; } = new List<Resource>();
    }

    [Serializable]
    public class Resource
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }
    }

    [Serializable]
    public class ReadResourceParams
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }
    }

    [Serializable]
    public class ReadResourceResult
    {
        [JsonProperty("contents")]
        public List<ResourceContents> Contents { get; set; } = new List<ResourceContents>();
    }

    [Serializable]
    public class ResourceContents
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("blob", NullValueHandling = NullValueHandling.Ignore)]
        public string Blob { get; set; }
    }

    // ==================== Prompts ====================

    [Serializable]
    public class ListPromptsResult
    {
        [JsonProperty("prompts")]
        public List<Prompt> Prompts { get; set; } = new List<Prompt>();
    }

    [Serializable]
    public class Prompt
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public List<PromptArgument> Arguments { get; set; }
    }

    [Serializable]
    public class PromptArgument
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Required { get; set; }
    }

    [Serializable]
    public class GetPromptParams
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments")]
        public Dictionary<string, string> Arguments { get; set; }
    }

    [Serializable]
    public class GetPromptResult
    {
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("messages")]
        public List<PromptMessage> Messages { get; set; } = new List<PromptMessage>();
    }

    [Serializable]
    public class PromptMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } // "user", "assistant"

        [JsonProperty("content")]
        public Content Content { get; set; }
    }

    // ==================== 错误码 ====================

    public static class JsonRpcErrorCodes
    {
        public const int ParseError = -32700;
        public const int InvalidRequest = -32600;
        public const int MethodNotFound = -32601;
        public const int InvalidParams = -32602;
        public const int InternalError = -32603;
    }
}
