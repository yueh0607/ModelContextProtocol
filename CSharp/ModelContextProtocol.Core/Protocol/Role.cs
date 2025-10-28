using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议对话中的角色类型。
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Role
    {
        /// <summary>
        /// 对应于对话中的人类用户。
        /// </summary>
        [EnumMember(Value = "user")]
        User,
        
        /// <summary>
        /// 对应对话中的AI助手。
        /// </summary>
        [EnumMember(Value = "assistant")]
        Assistant
    }
}