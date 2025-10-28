namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端通过 <see cref="RequestMethods.ToolsList"/> 请求获取服务器可用工具列表时使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器返回一个包含可用工具的 <see cref="ListToolsResult"/> 响应。
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class ListToolsRequestParams : PaginatedRequestParams
    {
        
    }
}