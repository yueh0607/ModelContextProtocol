using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端通过 <see cref="RequestMethods.ToolsCall"/> 请求调用服务器提供的工具时使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器将响应一个包含工具调用结果的 <see cref="CallToolResult"/>。
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class CallToolRequestParams : RequestParams
    {
        /// <summary>获取或设置要调用的工具的名称。</summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置在服务器上调用工具时传递给该工具的可选参数。
        /// </summary>
        /// <remarks>
        /// 此对象包含要传递给工具的参数值。
        /// 使用 JObject 以支持任意类型的参数（字符串、数字、对象、数组等）。
        /// </remarks>
        [JsonProperty("arguments")]
        public JObject Arguments { get; set; }
    }
}