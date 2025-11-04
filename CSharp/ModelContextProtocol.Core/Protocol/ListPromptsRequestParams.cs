namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端使用 <see cref="RequestMethods.PromptsList"/> 请求服务器可用提示列表时所使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器会返回一个包含可用提示的 <see cref="ListPromptsResult"/>。
    /// 有关详细信息，请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/>schema</see>。
    /// </remarks>
    public class ListPromptsRequestParams : PaginatedRequestParams
    {

    }
}