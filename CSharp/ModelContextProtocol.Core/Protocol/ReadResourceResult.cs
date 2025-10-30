using System.Collections.Generic;
using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器对客户端发起的<see cref="RequestMethods.ResourcesRead"/>请求的响应。
    /// </summary>
    /// <remarks>
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public class ReadResourceResult : Result
    {
        /// <summary>
        /// 获取或设置本资源包含的<see cref="ResourceContents"/>对象列表。
        /// </summary>
        /// <remarks>
        /// 此属性包含所请求资源的实际内容，该内容可以是
        /// 文本型（<see cref="TextResourceContents"/>）或二进制型（<see cref="BlobResourceContents"/>）。
        /// 包含的内容类型取决于正在访问的资源。
        /// </remarks>
        [JsonProperty("contents")]
        public IList<ResourceContents> Contents { get; set; } = new List<ResourceContents>();
    }
}