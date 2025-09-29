using Newtonsoft.Json;

namespace JsonRpc.Models
{
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public object Id { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params { get; set; }

        public bool IsNotification => Id == null;
    }
}