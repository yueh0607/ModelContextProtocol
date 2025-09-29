using Newtonsoft.Json;
using System.Collections.Generic;

namespace McpServerLib.Mcp.Models
{
    public class ListToolsResponse
    {
        [JsonProperty("tools")]
        public List<Tool> Tools { get; set; }

        public ListToolsResponse()
        {
            Tools = new List<Tool>();
        }
    }

    public class Tool
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("inputSchema")]
        public ToolInputSchema InputSchema { get; set; }

        public Tool()
        {
            Name = string.Empty;
            Description = string.Empty;
            InputSchema = new ToolInputSchema();
        }
    }

    public class ToolInputSchema
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Properties { get; set; }

        [JsonProperty("required", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Required { get; set; }

        public ToolInputSchema()
        {
            Type = "object";
            Properties = new Dictionary<string, object>();
            Required = new List<string>();
        }
    }

    public class CallToolRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Arguments { get; set; }

        public CallToolRequest()
        {
            Name = string.Empty;
            Arguments = new Dictionary<string, object>();
        }
    }

    public class CallToolResponse
    {
        [JsonProperty("content")]
        public List<Content> Content { get; set; }

        [JsonProperty("isError", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsError { get; set; }

        public CallToolResponse()
        {
            Content = new List<Content>();
        }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        public Content()
        {
            Type = "text";
            Text = string.Empty;
        }
    }
}