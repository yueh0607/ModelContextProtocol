using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 为分页请求提供基类。
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">详情请参阅架构</see>
    /// </remarks>
    public class PaginatedRequestParams : RequestParams

    {
    /// <summary>防止外部派生。</summary>
    private protected PaginatedRequestParams()
    {
    }

    /// <summary>
    /// 获取或设置表示当前分页位置的不透明令牌。
    /// </summary>
    /// <remarks>
    /// 如果提供，服务器应返回从此游标之后开始的结果。
    /// 此值应从上一个请求响应的 <see cref="PaginatedResult.NextCursor"/>
    /// 属性中获取。
    /// </remarks>
    [JsonProperty("cursor")]
    public string Cursor { get; set; }
    }
}