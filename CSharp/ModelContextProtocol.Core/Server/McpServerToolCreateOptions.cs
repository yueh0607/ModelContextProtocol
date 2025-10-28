using System;
using System.Collections.Generic;
using System.ComponentModel;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// 提供用于控制 <see cref="McpServerTool"/> 创建的选项。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这些选项允许自定义使用
    /// <see cref="M:McpServerTool.Create"/> 创建的工具的行为和元数据。
    /// 它们可以控制命名、描述、工具属性和依赖注入集成。
    /// </para>
    /// <para>
    /// 当以编程方式创建工具而不是使用属性时，
    /// 这些选项提供相同级别的配置灵活性。
    /// </para>
    /// </remarks>
    public sealed class McpServerToolCreateOptions
    {
        /// <summary>
        /// 获取或设置用于构建 <see cref="McpServerTool"/> 的可选服务。
        /// </summary>
        /// <remarks>
        /// 这些服务将用于确定哪些参数需要通过依赖注入来满足。因此，
        /// 通过此提供程序满足的服务应与调用时传入的提供程序满足的服务相匹配。
        /// </remarks>
        public IServiceProvider Services { get; set; }

        /// <summary>
        /// 获取或设置 <see cref="McpServerTool"/> 的名称。
        /// </summary>
        /// <remarks>
        /// 如果 <see langword="null"/>，但方法应用了 <see cref="McpServerToolAttribute"/>，
        /// 将使用属性中的名称。如果不存在属性，则使用基于方法名称的名称。
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置用于 <see cref="McpServerTool"/> 的描述。
        /// </summary>
        /// <remarks>
        /// 如果 <see langword="null"/>，但方法应用了 <see cref="DescriptionAttribute"/>，
        /// 将使用该属性的描述。
        /// </remarks>
        public string Description { get; set; }

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
        public bool Destructive { get; set; }

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
        public bool Idempotent { get; set; }

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
        public bool OpenWorld { get; set; }

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
        public bool ReadOnly { get; set; }

        /// <summary>
        /// 获取或设置工具是否应报告结构化内容的输出架构。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 启用后，工具将尝试填充 <see cref="Tool.OutputSchema"/>
        /// 并在 <see cref="CallToolResult.StructuredContent"/> 属性中提供结构化内容。
        /// </para>
        /// <para>
        /// 默认值为 <see langword="false"/>。
        /// </para>
        /// </remarks>
        public bool UseStructuredContent { get; set; }

        /// <summary>
        /// 获取或设置在将数据编组到 JSON 或从 JSON 编组数据时使用的 JSON 序列化器选项。
        /// </summary>
        /// <remarks>
        /// 如果未指定，则默认为 <see cref="McpJsonUtilities.DefaultSettings"/>。
        /// </remarks>
        public JsonSerializerSettings SerializerOptions { get; set; }

        /// <summary>
        /// 从方法创建 <see cref="AIFunction"/> 时获取或设置 JSON 模式选项。
        /// </summary>
        /// <remarks>
        /// 如果未指定，则默认为 <see cref="AIJsonSchemaCreateOptions.Default"/>。
        /// </remarks>
        public AIJsonSchemaCreateOptions? SchemaCreateOptions { get; set; }

        /// <summary>
        /// 获取或设置与工具关联的元数据。
        /// </summary>
        /// <remarks>
        /// 元数据包含从方法及其声明类中提取的属性等信息。
        /// 如果未提供，则将为通过反射创建的方法自动生成元数据。
        /// </remarks>
        public IReadOnlyList<object> Metadata { get; set; }

        /// <summary>
        /// 获取或设置此工具的图标。
        /// </summary>
        /// <remarks>
        /// 客户端可以使用它来在用户界面中显示工具的图标。
        /// </remarks>
        public IList<Icon> Icons { get; set; }

        /// <summary>
        /// 创建当前 <see cref="McpServerToolCreateOptions"/> 实例的浅克隆。
        /// </summary>
        internal McpServerToolCreateOptions Clone() =>
            new McpServerToolCreateOptions
            {
                Services = Services,
                Name = Name,
                Description = Description,
                Title = Title,
                Destructive = Destructive,
                Idempotent = Idempotent,
                OpenWorld = OpenWorld,
                ReadOnly = ReadOnly,
                UseStructuredContent = UseStructuredContent,
                SerializerOptions = SerializerOptions,
                SchemaCreateOptions = SchemaCreateOptions,
                Metadata = Metadata,
                Icons = Icons,
            };
        }
}