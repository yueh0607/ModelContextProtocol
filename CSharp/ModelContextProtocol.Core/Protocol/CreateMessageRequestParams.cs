using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示与 <see cref="RequestMethods.SamplingCreateMessage"/> 一起使用的参数
    /// 来自服务器的请求，用于通过客户端对 LLM 进行采样。
    /// </summary>
    /// <remarks>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class CreateMessageRequestParams : RequestParams
    {
        /// <summary>
        /// 获取或设置提示中应包含哪些服务器上下文的指示。
        /// </summary>
        /// <remarks>
        /// 客户端可能会忽略此请求。
        /// </remarks>
        [JsonProperty("includeContext")]
        public ContextInclusion? IncludeContext { get; set; }

        /// <summary>
        /// 获取或设置要传递给 LLM 提供程序的可选元数据。
        /// </summary>
        /// <remarks>
        /// 此元数据的格式特定于提供程序，可以包含特定于模型的设置或标准参数未涵盖的配置。
        /// 这允许传递特定于某些 AI 模型或提供程序的自定义参数。
        /// </remarks>
        [JsonProperty("maxTokens")]
        public int? MaxTokens { get; set; }


        /// <summary>
        /// 获取或设置服务器请求包含在提示中的消息。
        /// </summary>
        [JsonProperty("messages", Required = Required.Always)]
        public IReadOnlyList<SamplingMessage> Messages { get; set; }


        /// <summary>
        /// 获取或设置要传递给 LLM 提供程序的可选元数据。
        /// </summary>
        /// <remarks>
        /// 此元数据的格式特定于提供程序，可以包含特定于模型的设置或标准参数未涵盖的配置。
        /// 这允许传递特定于某些 AI 模型或提供程序的自定义参数。
        /// </remarks>
        [JsonProperty("metadata")]
        public JToken Metadata { get; set; }

        /// <summary>
        /// 获取或设置服务器对要选择的模型的偏好设置。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 客户端可以忽略这些偏好设置。
        /// </para>
        /// <para>
        /// 这些偏好设置可帮助客户端根据服务器的优先级（成本、速度、智能和特定模型提示）选择合适的模型。
        /// </para>
        /// <para>
        /// 当指定多个维度（成本、速度、智能）时，客户端应根据这些维度的相对值进行平衡。
        /// 如果提供了特定的模型提示，客户端应按顺序评估它们，并优先于数字优先级。
        /// </para>
        /// </remarks>
        [JsonProperty("modelPreferences")]
        public ModelPreferences ModelPreferences { get; set; }

        /// <summary>
        /// 获取或设置可选字符序列，用于指示 LLM 在遇到这些序列时停止生成文本。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当模型在采样过程中生成任何这些序列时，文本生成都会立即停止，
        /// 即使未达到最大标记数限制。这对于控制生成
        /// 结尾或防止模型继续超出某些点非常有用。
        /// </para>
        /// <para>
        /// 停止序列通常区分大小写，并且通常只有当生成的
        /// 序列与提供的序列之一完全匹配时，LLM 才会停止生成。常见用途包括结束标记（如“END”）、标点符号
        /// （如“.”）或特殊分隔符序列（如“###”）。
        /// </para>
        /// </remarks>
        [JsonProperty("stopSequences")]
        public IReadOnlyList<string> StopSequences { get; set; }


        /// <summary>
        /// 获取或设置服务器用于采样的可选系统提示。
        /// </summary>
        /// <remarks>
        /// 客户端可以修改或省略此提示。
        /// </remarks>
        [JsonProperty("systemPrompt")]
        public string SystemPrompt { get; set; }

        /// <summary>
        /// 获取或设置服务器请求用于采样的温度值。
        /// </summary>
        /// <remarks>
        /// 温度控制生成文本的随机性：
        /// 较低温度(如 0~0.3): 更确定、保守。
        /// 中等温度(0.7 默认附近): 平衡。
        /// 高温度(>1.0): 更随机、多样, 也更可能失真。
        /// </remarks>
        [JsonProperty("temperature")]
        public float? Temperature { get; set; }
    }
}