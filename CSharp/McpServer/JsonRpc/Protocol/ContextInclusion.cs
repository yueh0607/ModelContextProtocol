using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 指定模型上下文协议 (MCP) 中请求的上下文包含选项。
    /// </summary>
    /// <remarks>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContextInclusion
    {
        /// <summary>
        /// 表示不应包含上下文。
        /// </summary>
        [EnumMember(Value = "none")]
        None,

        /// <summary>
        /// 表示应包含发送请求的服务器的上下文。
        /// </summary>
        [EnumMember(Value = "thisServer")]
        ThisServer,

        /// <summary>
        /// 表示应包含客户端连接的所有服务器的上下文。
        /// </summary>
        [EnumMember(Value = "allServers")]
        AllServers
    }
}