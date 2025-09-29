using Newtonsoft.Json;

namespace McpServerLib.Mcp.Models
{
    public class InitializeRequest
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ClientCapabilities Capabilities { get; set; }

        [JsonProperty("clientInfo")]
        public ClientInfo ClientInfo { get; set; }

        public InitializeRequest()
        {
            ProtocolVersion = "2024-11-05";
            Capabilities = new ClientCapabilities();
            ClientInfo = new ClientInfo();
        }
    }

    public class InitializeResponse
    {
        [JsonProperty("protocolVersion")]
        public string ProtocolVersion { get; set; }

        [JsonProperty("capabilities")]
        public ServerCapabilities Capabilities { get; set; }

        [JsonProperty("serverInfo")]
        public ServerInfo ServerInfo { get; set; }

        public InitializeResponse()
        {
            ProtocolVersion = "2024-11-05";
            Capabilities = new ServerCapabilities();
            ServerInfo = new ServerInfo();
        }
    }

    public class ClientCapabilities
    {
        [JsonProperty("roots", NullValueHandling = NullValueHandling.Ignore)]
        public RootsCapability Roots { get; set; }

        [JsonProperty("sampling", NullValueHandling = NullValueHandling.Ignore)]
        public object Sampling { get; set; }
    }

    public class ServerCapabilities
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public ToolsCapability Tools { get; set; }

        [JsonProperty("resources", NullValueHandling = NullValueHandling.Ignore)]
        public ResourcesCapability Resources { get; set; }

        [JsonProperty("prompts", NullValueHandling = NullValueHandling.Ignore)]
        public PromptsCapability Prompts { get; set; }

        [JsonProperty("logging", NullValueHandling = NullValueHandling.Ignore)]
        public object Logging { get; set; }
    }

    public class RootsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    public class ToolsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    public class ResourcesCapability
    {
        [JsonProperty("subscribe", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Subscribe { get; set; }

        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    public class PromptsCapability
    {
        [JsonProperty("listChanged", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ListChanged { get; set; }
    }

    public class ClientInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public ClientInfo()
        {
            Name = "Unknown Client";
            Version = "1.0.0";
        }
    }

    public class ServerInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        public ServerInfo()
        {
            Name = "MCP Server";
            Version = "1.0.0";
        }
    }
}