namespace ModelContextProtocol.Protocol
{
    public class ListToolsResult
    {
        /// <summary>
        /// The server's response to a tools/list request from the client.
        /// </summary>
        [JsonPropertyName("tools")]
        public IList<Tool> Tools { get; set; } = [];
    }
}