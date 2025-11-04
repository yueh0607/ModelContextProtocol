using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 提供用于模型选择的提示。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当在 <see cref="ModelPreferences.Hints"/> 中指定多个提示时，它们将按顺序进行评估，第一个匹配的提示优先。
    /// 客户端应优先考虑这些提示，而不是数字优先级。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class ModelHint
    {
        /// <summary>
        /// 获取或设置模型名称的提示。
        /// </summary>
        /// <remarks>
        /// 指定的字符串可以是部分或完整的模型名称。客户端也可以
        /// 将提示映射到来自不同提供商的等效模型。客户端会根据这些偏好及其可用的模型做出最终的模型选择。
        /// 最终模型选择。
        /// </remarks>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}