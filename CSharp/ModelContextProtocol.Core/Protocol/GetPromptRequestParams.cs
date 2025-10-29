using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示客户端通过 <see cref="RequestMethods.PromptsGet"/> 请求获取服务器提供的提示时使用的参数。
    /// </summary>
    /// <remarks>
    /// 服务器将响应一个包含提示结果的 <see cref="GetPromptResult"/>。
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public sealed class GetPromptRequestParams : RequestParams
    {
        /// <summary>
        /// 获取或设置提示的名称。
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置用于在从服务器检索提示时对提示进行模板化的参数。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 通常，这些参数用于替换提示模板中的占位符。此字典中的键
        /// 应与提示的 <see cref="Prompt.Arguments"/> 列表中定义的名称匹配。但是，服务器可以
        /// 选择以它认为适当的任何方式使用这些参数来生成提示。
        /// </para>
        /// <para>
        /// 这些参数是示例化提示时的输入值，允许客户端为提示模板中的变量提供具体值。
        /// </para>
        /// </remarks>
        [JsonProperty("arguments")]
        public Dictionary<string, JToken> Arguments { get; set; } // Newtonsoft.Json无法序列化 IReadOnlyDictionary
    }
}