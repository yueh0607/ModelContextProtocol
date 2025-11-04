using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器提供的提示。
    /// </summary>
    /// <remarks>
    /// 详情请参阅<see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">架构说明</see>。
    /// </remarks>
    public class Prompt : IBaseMetadata
    {
        /// <inheritdoc />
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <inheritdoc />
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置此提示提供的可选说明。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此说明有助于开发人员理解提示的目的和使用场景。
        /// 应阐明提示的设计目标及任何重要背景信息。
        /// </para>
        /// <para>
        /// 该描述通常用于文档说明、用户界面展示，以及为客户端应用程序提供上下文信息，
        /// 这些应用程序可能需要在多个可用提示中进行选择。
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 获取或设置此提示接受的用于模板化和自定义的参数列表。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此列表定义了请求提示时可提供的参数。
        /// 每个参数指定名称、描述及是否必填等元数据。
        /// </para>
        /// <para>
        /// 当客户端发起<see cref="RequestMethods.PromptsGet"/>请求时，可为这些参数提供值，
        /// 这些值将被替换到提示模板中或用于渲染提示内容。
        /// </para>
        /// </remarks>
        [JsonProperty("arguments")]
        public IList<PromptArgument> Arguments { get; set; }

        /// <summary>
        /// 获取或设置此提示符的可选图标列表。
        /// </summary>
        /// <remarks>
        /// 客户端可使用此功能在用户界面中显示提示符的图标。
        /// </remarks>
        [JsonProperty("icons")]
        public IList<Icon> Icons { get; set; }

        /// <summary>
        /// 获取或设置由 MCP 为协议级元数据保留的元数据。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容进行任何假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }

        // /// <summary>
        // /// 获取或设置与该元数据对应的可调用服务器提示符（如有）。
        // /// </summary>
        // [JsonIgnore]
        // public McpServerPrompt McpServerPrompt { get; set; }
    }
}