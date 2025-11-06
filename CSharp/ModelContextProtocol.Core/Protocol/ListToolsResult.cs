using System.Collections.Generic;

using ModelContextProtocol.Json;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器对客户端发出的 <see cref="RequestMethods.ToolsList"/> 请求的响应。
    /// </summary>
    public class ListToolsResult : Result
    {
        /// <summary>
        /// 获取或设置可用工具列表。
        /// </summary>
        [JsonProperty("tools")]
        public IList<Tool> Tools { get; set; } = new List<Tool>();
    }
}