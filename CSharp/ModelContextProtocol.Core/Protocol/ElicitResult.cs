using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端对 Elicit 请求的响应。
    /// </summary>
    public sealed class ElicitResult : Result
    {
        /// <summary>
        /// 获取或设置用户响应引导的操作。
        /// </summary>
        /// <value>
        /// 如果未明确设置，则默认为“取消”。
        /// </value>
        /// <remarks>
        /// <list type="bullet">
        /// <item>
        /// <term>"accept"</term>
        /// <description>用户提交表单/确认操作</description>
        /// </item>
        /// <item>
        /// <term>"decline"</term>
        /// <description>用户明确拒绝操作</description>
        /// </item>
        /// <item>
        /// <term>"cancel"</term>
        /// <description>用户未做出明确选择就被关闭（默认）</description>
        /// </item>
        /// </list>
        /// </remarks>
        [JsonProperty("action")]
        public string Action { get; set; } = "cancel";

        /// <summary>
        /// 便捷指示用户是否接受了引导。
        /// </summary>
        /// <remarks>
        /// 表示引导请求已成功完成，并且 <see cref="Content"/> 的值已填充。
        /// </remarks>
        [JsonIgnore]
        public bool IsAccepted => string.Equals(Action, "accept", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置已提交的表单数据。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 如果操作为“取消”或“拒绝”，则通常会省略此部分。
        /// </para>
        /// <para>
        /// 字典中的值应为 <see cref="string"/>、<see cref="JTokenType.Integer"/> / <see cref="JTokenType.Float"/>、
        /// <see cref="JTokenType.Boolean"/> 或 <see cref="JTokenType.Null"/> 类型。
        /// </para>
        /// </remarks>
        [JsonProperty("content")]
        public IDictionary<string, JToken> Content { get; set; }
    }

    /// <summary>
    /// 表示客户端对引导请求的响应，包含类型化的内容负载。
    /// </summary>
    /// <typeparam name="T">预期内容负载的类型。</typeparam>
    public sealed class ElicitResult<T> : Result
    {
        /// <summary>
        /// 获取或设置响应引导的用户操作。
        /// </summary>
        [JsonProperty("action")]
        public string Action { get; set; } = "cancel";

        /// <summary>
        /// 便捷指示用户是否接受了引导。
        /// </summary>
        /// <remarks>
        /// 表示引导请求已成功完成，并且 <see cref="Content"/> 的值已填充。
        /// </remarks>
        [JsonIgnore]
        public bool IsAccepted => string.Equals(Action, "accept", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置已提交的表单数据作为输入值。
        /// </summary>
        [JsonProperty("content")]
        public T Content { get; set; }
    }
}