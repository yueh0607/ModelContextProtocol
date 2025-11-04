using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器在采样期间向客户端请求的模型选择偏好。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 由于 LLM 可以在多个维度上变化，因此选择“最佳”模型并非易事。
    /// 不同的模型在不同的领域表现出色——有些模型速度更快但性能较差，有些模型性能更强但成本更高等等。
    /// 此类允许服务器在多个维度上表达其优先级，以帮助客户端根据其用例做出适当的选择。
    /// </para>
    /// <para>
    /// 这些偏好始终是建议性的，客户端可以忽略它们。
    /// 此外，客户端还可以决定如何解释这些偏好以及如何权衡它们与其他考虑因素。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public sealed class ModelPreferences
    {
        /// <summary>
        /// 获取或设置在选择模型时成本的优先级。
        /// </summary>
        /// <remarks>
        /// 值为 0 表示成本不重要，值为 1 表示成本是最重要的因素。
        /// </remarks>
        [JsonProperty("costPriority")]
        public float? CostPriority { get; set; }

        /// <summary>
        /// 获取或设置用于模型选择的可选提示。
        /// </summary>
        [JsonProperty("hints")]
        public IReadOnlyList<ModelHint> Hints { get; set; }

        /// <summary>
        /// 获取或设置在选择模型时优先考虑采样速度（延迟）的程度。
        /// </summary>
        /// <remarks>
        /// 值为 0 表示速度不重要，值为 1 表示速度是最重要的因素。
        /// </remarks>
        [JsonProperty("speedPriority")]
        public float? SpeedPriority { get; set; }

        /// <summary>
        /// Gets or sets how much to prioritize intelligence and capabilities when selecting a model.
        /// </summary>
        /// <remarks>
        /// A value of 0 means intelligence is not important, while a value of 1 means intelligence is the most important factor.
        /// </remarks>
        [JsonProperty("intelligencePriority")]
        public float? IntelligencePriority { get; set; }
    }
}