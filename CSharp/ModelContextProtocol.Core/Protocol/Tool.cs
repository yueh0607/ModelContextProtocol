using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示服务器能够调用的工具。
    /// </summary>
    public class Tool : IBaseMetadata
    {
        /// <inheritdoc />
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        /// <inheritdoc />
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// 获取或设置工具的可读描述。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此描述有助于 AI 模型理解该工具的功能以及何时使用。
        /// 它应该清晰、简洁、准确地描述工具的用途和功能。
        /// </para>
        /// <para>
        /// 此描述通常呈现给 AI 模型，以帮助它们根据用户请求确定何时以及如何使用该工具。
        /// </para>
        /// </remarks>
        [JsonProperty("description")]
        public string Description { get; set; }

        
        
        private JToken _inputSchema = McpJsonUtilities.DefaultMcpToolSchema;
        /// <summary>
        /// 获取或设置一个 JSON Schema 对象，用于定义工具的预期参数。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 该 Schema 必须是有效的 JSON Schema 对象，且其“type”属性设置为“object”。
        /// 如果提供的 Schema 无效，则通过 setter 中的验证来强制执行，并抛出 <see cref="ArgumentException"/>异常。
        /// </para>
        /// <para>
        /// Schema 通常定义工具接受的属性（参数）、 它们的类型以及哪些是必需的。
        /// 这有助于 AI 模型理解如何构建对工具的调用。
        /// </para>
        /// <para>
        /// 如果未明确设置，则使用默认的最小 Schema <c>{"type":"object"}</c>。
        /// </para>
        /// </remarks>
        [JsonProperty("inputSchema",Required = Required.Always)]
        public JToken InputSchema
        {
            get => _inputSchema;
            set
            {
                if (!McpJsonUtilities.IsValidMcpToolSchema(value))
                {
                    throw new ArgumentException("The specified document is not a valid MCP tool input JSON schema.", nameof(InputSchema));
                }

                _inputSchema = value;
            }

        }

        
        private JToken _outputSchema;
        /// <summary>
        /// 获取或设置一个 JSON Schema 对象，用于定义工具的预期结构化输出。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 该 Schema 必须是有效的 JSON Schema 对象，且其“type”属性设置为“object”。
        /// 如果提供的 Schema 无效，则通过 setter 中的验证来强制执行，抛出 <see cref="ArgumentException"/>
        /// 如果提供的 Schema 无效，则抛出异常。
        /// </para>
        /// <para>
        /// 该 Schema 应描述 <see cref="CallToolResult.StructuredContent"/> 中返回的数据形状。
        /// </para>
        /// </remarks>
        [JsonProperty("outputSchema")]
        public JToken OutputSchema
        {
            get => _outputSchema;
            set
            {
                if (value != null && !McpJsonUtilities.IsValidMcpToolSchema(value))
                {
                    throw new ArgumentException("The specified document is not a valid MCP tool output JSON schema.", nameof(OutputSchema));
                }

                _outputSchema = value;
            }
        }

        /// <summary>
        /// 获取或设置可选的附加工具信息和行为提示。
        /// </summary>
        /// <remarks>
        /// 这些注释提供有关工具行为的元数据，例如它是只读的、破坏性的、幂等的，还是在开放世界中运行。
        /// 它们还可以包含一个人类可读的标题。
        /// 请注意，这些只是提示，不应作为安全决策的依据。
        /// </remarks>
        [JsonProperty("annotations")]
        public ToolAnnotations Annotations { get; set; }

        /// <summary>
        /// 获取或设置此工具的可选图标列表。
        /// </summary>
        /// <remarks>
        /// 客户端可以使用它来在用户界面中显示工具的图标。
        /// </remarks>
        [JsonProperty("icons")]
        public IList<Icon> Icons { get; set; }

        /// <summary>
        /// 获取或设置 MCP 为协议级元数据保留的元数据。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容做出假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }

        /// <summary>
        /// 获取或设置与此元数据对应的可调用服务器工具（如果有）。
        /// </summary>
        [JsonIgnore]
        public McpServerTool? McpServerTool { get; set; }
    }
}