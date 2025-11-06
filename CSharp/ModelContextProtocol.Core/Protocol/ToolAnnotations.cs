using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示向客户端描述 <see cref="Tool"/> 的附加属性。
    /// </summary>
    /// <remarks>
    /// <see cref="ToolAnnotations"/> 中的所有属性均为提示。
    /// 它们不保证提供工具行为的忠实描述（包括“title”等描述性属性）。
    /// 客户端不应根据从不受信任的服务器收到的 <see cref="ToolAnnotations"/> 来做出工具使用决策。
    /// </remarks>
    public sealed class ToolAnnotations
    {
        /// <summary>
        /// 获取或设置可向用户显示的工具可读标题。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 标题为工具提供了一个比工具的程序化名称更具描述性、用户友好的名称。
        /// 标题旨在用于显示目的，并帮助用户一目了然地了解 工具的用途。
        /// </para>
        /// <para>
        /// 与工具名称（遵循程序化命名约定）不同，标题可以包含空格、特殊字符，并以更自然的语言风格表达。
        /// </para>
        /// </remarks>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置工具是否可以对其环境执行破坏性更新。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 如果 <see langword="true"/>，则该工具可以对其环境执行破坏性更新。
        /// 如果 <see langword="false"/>，则该工具仅执行附加更新。
        /// 当工具修改其环境（ReadOnly = false）时，此属性最为重要。
        /// </para>
        /// <para>
        /// 默认值为 <see langword="true"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("destructiveHint")]
        public bool DestructiveHint { get; set; }

        /// <summary>
        /// 获取或设置使用相同参数重复调用该工具是否不会对其环境产生额外影响。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当工具修改其环境 (ReadOnly = false) 时，此属性最为重要。
        /// </para>
        /// <para>
        /// 默认值为 <see langword="false"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("idempotentHint")]
        public bool IdempotentHint { get; set; }

        /// <summary>
        /// 获取或设置此工具是否可以与外部实体的“开放世界”交互。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 如果 <see langword="true"/>，则该工具可能与一组不可预测或动态的实体交互（例如网页搜索）。
        /// 如果 <see langword="false"/>，则该工具的交互域是封闭且定义明确的（例如内存访问）。
        /// </para>
        /// <para>
        /// 默认值为 <see langword="true"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("openWorldHint")]
        public bool OpenWorldHint { get; set; }

        /// <summary>
        /// 获取或设置此工具是否不修改其环境。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 如果 <see langword="true"/>，则该工具仅执行读取操作而不更改状态。
        /// 如果 <see langword="false"/>，则该工具可能会修改其环境。
        /// </para>
        /// <para>
        /// 只读工具除了计算资源使用之外没有副作用。
        /// 它们不会在任何系统中创建、更新或删除数据。
        /// </para>
        /// <para>
        /// 默认值为 <see langword="false"/>。
        /// </para>
        /// </remarks>
        [JsonProperty("readOnlyHint")]
        public bool ReadOnlyHint { get; set; }
    }
}