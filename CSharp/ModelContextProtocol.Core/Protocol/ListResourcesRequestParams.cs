namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端使用<see cref="RequestMethods.ResourcesList"/>请求向服务器请求可用资源列表时使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器将返回包含可用资源的<see cref="ListResourcesResult"/>响应。
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public class ListResourcesRequestParams : PaginatedRequestParams
    {

    }
}