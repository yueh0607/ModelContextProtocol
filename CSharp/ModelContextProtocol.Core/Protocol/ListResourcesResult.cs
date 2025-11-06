using System.Collections.Generic;

using ModelContextProtocol.Json;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器对客户端发起的<see cref="RequestMethods.ResourcesList"/>请求的响应，包含可用资源列表。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当客户端发送<see cref="RequestMethods. ResourcesList"/> 请求以发现服务器上可用资源时返回。
    /// </para>
    /// <para>
    /// 它继承自 <see cref="PaginatedResult"/>，当资源数量庞大时可实现分页响应。
    /// 服务器可提供 <see cref="PaginatedResult.NextCursor"/> 属性，指示当前响应之外存在更多可用资源。
    /// </para>
    /// <para>
    /// 参见 <see href="https://github.com/modelcontextprotocol/specification/blob/main/sche"/>。
    /// 当前响应之外存在更多可用资源。
    /// </para>
    /// <para>
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public class ListResourcesResult : PaginatedResult
    {
        /// <summary>
        /// 服务器提供的资源列表。
        /// </summary>
        [JsonProperty("resources")]
        public IList<Resource> Resources { get; set; } = new List<Resource>();
    }
}