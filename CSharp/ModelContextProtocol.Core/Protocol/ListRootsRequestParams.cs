namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器发送 <see cref="RequestMethods.ResourcesTemplatesList"/> 请求时使用的参数，用于请求客户端可用的根列表。
    /// </summary>
    /// <remarks>
    /// 客户端响应一个包含客户端根的 <see cref="ListRootsResult"/> 请求。
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class ListRootsRequestParams : RequestParams
    {

    }

}