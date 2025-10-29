using Newtonsoft.Json;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示提示符可用于模板化和自定义的参数。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PromptArgument"/> 类定义了可提供给提示的参数元数据。
    /// 当通过 <see cref="RequestMethods.PromptsGet"/> 请求检索提示时，
    /// 这些参数用于定制或参数化提示内容。
    /// </para>
    /// <para>
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </para>
    /// </remarks>
    public class PromptArgument : IBaseMetadata
    {
        /// <inheritdoc />
        [JsonProperty("name",Required = Newtonsoft.Json.Required.Always)]
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置参数用途及预期值的人类可读描述。
        /// </summary>
        /// <remarks>
        /// 此描述可帮助开发者理解该参数应提供何种信息
        /// 以及其如何影响生成的提示语。
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置是否必须在请求提示时提供此参数的指示。
        /// </summary>
        /// <remarks>
        /// 当设置为<see langword="true"/>时，客户端在执行<see cref="RequestMethods.PromptsGet"/>请求时必须包含此参数。
        /// 若必填参数缺失，服务器应返回错误响应。
        /// </remarks>
        [JsonProperty("required")]
        public bool Required { get; set; }
    }
}