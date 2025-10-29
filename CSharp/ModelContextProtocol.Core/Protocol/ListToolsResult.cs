using System.Collections.Generic;
using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    public class ListToolsResult
    {
        /// <summary>
        /// The server's response to a tools/list request from the client.
        /// </summary>
        [JsonProperty("tools")]
        public IList<Tool> Tools { get; set; } = new List<Tool>();
    }
}