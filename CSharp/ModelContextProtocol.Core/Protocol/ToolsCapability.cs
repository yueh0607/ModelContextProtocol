using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器的工具功能，用于列出客户端能够调用的工具。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ToolsCapability"/> 指示服务器提供工具功能，允许客户端发现和调用服务器上的工具。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public sealed class ToolsCapability
    {
        /// <summary>
        /// 获取或设置一个值，指示工具列表是否会在运行时更改。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当设置为 <see langword="true"/> 时，表示服务器可能会在运行时动态添加、删除或修改工具。
        /// 客户端可以使用此信息来决定是否需要定期轮询工具列表，或者可以依赖服务器发送的通知。
        /// </para>
        /// <para>
        /// 当设置为 <see langword="false"/> 或未设置时，表示工具列表在服务器运行期间保持静态。
        /// </para>
        /// </remarks>
        [JsonProperty("listChanged")]
        public bool? ListChanged { get; set; }
    }
}

