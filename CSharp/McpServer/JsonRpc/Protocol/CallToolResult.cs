using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示客户端发出 <see cref="RequestMethods.ToolsCall"/> 请求调用服务器提供的工具的结果。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 任何源自该工具的错误都应在结果对象中报告，
    /// 并将 <see cref="IsError"/> 设置为 true，而不是报告为 <see cref="JsonRpcError"/>。
    /// </para>
    /// <para>
    /// 但是，任何查找工具时的错误、指示服务器不支持工具调用的错误或任何其他异常情况，
    /// 都应报告为 MCP 错误响应。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class CallToolResult : Result
    {
        /// <summary>
        /// 获取或设置工具调用的响应内容。
        /// </summary>
        [JsonProperty("content")]
        public IList<ContentBlock> Content { get; set; } = new List<ContentBlock>();
        
        /// <summary>
        /// 获取或设置一个可选的 JSON 对象，用于表示工具调用的结构化结果。
        /// </summary>
        [JsonProperty("structuredContent")]
        public JToken StructuredContent { get; set; }

        /// <summary>
        /// 获取或设置工具调用是否失败的指示。
        /// </summary>
        /// <remarks>
        /// 设置为 <see langword="true"/> 时，表示工具执行失败。
        /// 将此属性设置为 <see langword="true"/> 时，工具错误会报告，
        /// 详细信息请参见 <see cref="Content"/>属性，而不是协议级错误。
        /// 这使得 LLM 能够看到发生了错误，并可能在后续请求中自我纠正。
        /// </remarks>
        [JsonProperty("isError")]
        public bool IsError { get; set; }
    }
}