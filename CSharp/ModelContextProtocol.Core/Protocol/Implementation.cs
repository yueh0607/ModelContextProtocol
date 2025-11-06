using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 提供 MCP 实现的名称和版本。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Implementation"/> 类用于在初始化握手期间识别 MCP 客户端和服务器。
    /// 它提供版本和名称信息，可用于兼容性检查、日志记录和调试。
    /// </para>
    /// <para>
    /// 客户端和服务器在建立连接时都会提供此信息。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">通信结构定义</see>。
    /// </para>
    /// </remarks>
    public sealed class Implementation : IBaseMetadata
    {
        /// <inheritdoc />
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置实现的版本。
        /// </summary>
        /// <remarks>
        /// 该版本在客户端-服务器握手期间用于识别实现版本，
        /// 这对于解决兼容性问题或报告错误非常重要。
        /// </remarks>
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }

        /*
         * TODO：原代码可选实现了
         * public IList<Icon>? Icons { get; set; }
         * public string? WebsiteUrl { get; set; }
         * 但通信协议中不强制，所以先不实现。
        */
    }
}