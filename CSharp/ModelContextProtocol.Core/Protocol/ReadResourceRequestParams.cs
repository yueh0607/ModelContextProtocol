using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端使用<see cref="RequestMethods.ResourcesRead"/>请求获取服务器提供的资源时使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器将返回包含结果资源数据的<see cref="ReadResourceResult"/>响应。
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public class ReadResourceRequestParams : RequestParams
    {
        /// <summary>
        /// 要读取的资源的URI。该URI可使用任何协议；具体如何解析由服务器决定。
        /// </summary>
        [JsonProperty("uri", Required = Required.Always)]
        public string Uri { get; set; }
    }
}