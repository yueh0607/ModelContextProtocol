using Newtonsoft.Json;
using System.Collections.Generic;

namespace McpServerLib.Mcp.Models
{
    public class ListResourcesResponse
    {
        [JsonProperty("resources")]
        public List<Resource> Resources { get; set; }

        public ListResourcesResponse()
        {
            Resources = new List<Resource>();
        }
    }

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

        public Resource()
        {
            Uri = string.Empty;
            Name = string.Empty;
        }
    }

    public class ReadResourceRequest
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        public ReadResourceRequest()
        {
            Uri = string.Empty;
        }
    }

    public class ReadResourceResponse
    {
        [JsonProperty("contents")]
        public List<ResourceContent> Contents { get; set; }

        public ReadResourceResponse()
        {
            Contents = new List<ResourceContent>();
        }
    }

    public class ResourceContent
    {
        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string MimeType { get; set; }

        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty("blob", NullValueHandling = NullValueHandling.Ignore)]
        public string Blob { get; set; }

        public ResourceContent()
        {
            Uri = string.Empty;
        }
    }
}