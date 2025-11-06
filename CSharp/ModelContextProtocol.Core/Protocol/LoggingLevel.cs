using System.Runtime.Serialization;

using ModelContextProtocol.Json;
using ModelContextProtocol.Json.Converters;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 指示日志消息的严重性。
    /// </summary>
    /// <remarks>
    /// 这些映射到系统日志消息严重性，如
    /// <see href="https://datatracker.ietf.org/doc/html/rfc5424#section-6.2.1">RFC-5424</see> 中所述。
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LoggingLevel
    {
        /// <summary>详细的调试信息，通常仅对开发人员有价值。</summary>
        [EnumMember(Value = "debug")]
        Debug,

        /// <summary>无需采取任何行动的正常操作消息。</summary>
        [EnumMember(Value = "info")]
        Info,

        /// <summary>正常但重要的事件，可能值得关注。</summary>
        [EnumMember(Value = "notice")]
        Notice,

        /// <summary>警告条件不代表错误，但表明存在潜在问题。</summary>
        [EnumMember(Value = "warning")]
        Warning,

        /// <summary>应该解决但不需要立即采取行动的错误情况。</summary>
        [EnumMember(Value = "error")]
        Error,

        /// <summary>需要立即关注的危急情况。</summary>
        [EnumMember(Value = "critical")]
        Critical,

        /// <summary>必须立即采取措施解决该情况。</summary>
        [EnumMember(Value = "alert")]
        Alert,

        /// <summary>系统无法使用，需要立即关注。</summary>
        [EnumMember(Value = "emergency")]
        Emergency
    }
}