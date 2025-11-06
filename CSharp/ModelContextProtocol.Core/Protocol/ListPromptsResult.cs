using System.Collections.Generic;

using ModelContextProtocol.Json;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器对客户端发起的<see cref="RequestMethods.PromptsList"/>请求的响应，包含可用的提示。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当客户端发送<see cref="RequestMethods.PromptsList"/>请求以发现服务器上可用提示时，将返回此结果。
    /// </para>
    /// <para>
    /// 该结果继承自<see cref="PaginatedResult"/>，当提示词数量庞大时可实现分页响应。
    /// 服务器可提供<see cref="PaginatedResult.NextCursor"/>属性，表明当前响应之外尚有更多可用提示词。
    /// </para>
    /// <para>
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public class ListPromptsResult : PaginatedResult
    {
        /// <summary>
        /// 服务器提供的提示或提示模板列表。
        /// </summary>
        [JsonProperty("prompts")]
        public IList<Prompt> Prompts { get; set; } = new List<Prompt>();
    }
}