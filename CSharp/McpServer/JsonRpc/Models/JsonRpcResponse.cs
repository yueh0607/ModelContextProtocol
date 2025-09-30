using Newtonsoft.Json;

namespace JsonRpc.Models
{
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

        [JsonIgnore]
        public bool IsError => Error != null;

        [JsonIgnore]
        public bool IsSuccess => Error == null;
    }
}