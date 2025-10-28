using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Server
{
    
    /// <summary>
    /// 表示模型上下文协议客户端和服务器使用的可调用工具。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="McpServerTool"/> 是一个抽象基类，
    /// 表示在服务器中使用的 MCP 工具（与提供工具协议表示的 <see cref="Tool"/> 和 提供工具客户端表示的 <see cref="McpClientTool"/> 相反）。
    /// <see cref="McpServerTool"/> 的实例可以添加到 <see cref="IServiceCollection"/> 中，以便在使用 <see cref="McpServer"/> 创建 <see cref="McpServer"/> 时自动获取，
    /// 或者添加到 <see cref="McpServerPrimitiveCollection{McpServerTool}"/> 中。
    /// </para>
    /// <para>
    /// 最常见的是，<see cref="McpServerTool"/> 实例是使用静态方法 <see cref="M:McpServerTool.Create"/> 创建的。
    /// 这些方法支持为通过 <see cref="Delegate"/> 或 <see cref="MethodInfo"/> 指定的方法创建 <see cref="McpServerTool"/>，
    /// 并且这些方法会被 WithToolsFromAssembly 和 WithTools 隐式使用。
    /// <see cref="M:McpServerTool.Create"/> 方法创建的 <see cref="McpServerTool"/> 实例能够处理各种 .NET 方法签名，
    /// 并自动处理 如何将参数从 MCP 客户端接收的 JSON 数据编组到方法中，以及如何将返回值编组回
    /// <see cref="CallToolResult"/>，然后将其序列化并发送回客户端。
    /// </para>
    /// <para>
    /// 默认情况下，参数来源于 <see cref="CallToolRequestParams.Arguments"/> 字典，
    /// 该字典是一个键值对的集合 ，并以函数的 JSON 模式表示，如返回的 <see cref="McpServerTool"/> 的
    /// <see cref="ProtocolTool"/> 的 <see cref="Tool.InputSchema"/> 中所示。
    /// 这些参数是从该集合中的 <see cref="JsonElement"/> 值反序列化的。
    /// 有一些例外情况：
    /// <list type="bullet">
    ///     <item>
    ///         <description>
    ///             <see cref="CancellationToken"/> 参数会自动绑定到由<see cref="McpServer"/> 提供的 <see cref="CancellationToken"/>，
    ///             并且该绑定会遵循客户端为此操作发送的任何 <see cref="CancelledNotificationParams"/> <see cref="RequestId"/> 参数。
    ///             该参数不包含在生成的 JSON 架构中。
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="IServiceProvider"/> 参数会从此请求的 <see cref="RequestContext{CallToolRequestParams}"/> 绑定，
    ///             并且不包含在 JSON 架构中。
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="McpServer"/> 参数不包含在 JSON 架构中，
    ///             而是直接绑定到与此请求的 <see cref="RequestContext{CallToolRequestParams}"/> 关联的 <see cref="McpServer"/> 实例。
    ///             这些参数可用于了解正在使用哪个服务器来处理请求，并与向该服务器发出请求的客户端进行交互。
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             接受 <see cref="ProgressNotificationValue"/> 值的 <see cref="IProgress{ProgressNotificationValue}"/> 参数
    ///             不包含在 JSON 架构中，而是绑定到生成的 <see cref="IProgress{ProgressNotificationValue}"/> 实例，
    ///             用于将进度通知从工具转发到客户端。
    ///             如果客户端在其请求中包含 <see cref="ProgressToken"/>，
    ///             发送到此实例的进度报告将作为带有该令牌的 <see cref="NotificationMethods.ProgressNotification"/> 通知传播到客户端。
    ///             如果客户端未包含 <see cref="ProgressToken"/>，则实例将忽略发送给它的任何进度报告。
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             构建 <see cref="McpServerTool"/> 时，可以通过
    ///             <see cref="McpServerToolCreateOptions.Services"/> 传递 <see cref="IServiceProvider"/>。
    ///             任何符合 <see cref="IServiceProvider"/> 要求的参数，
    ///             根据 <see cref="IServiceProviderIsService"/> 的规则，
    ///             都不会包含在生成的 JSON Schema 中，而是会从提供给 <see cref="InvokeAsync"/> 的参数集合中进行解析。
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             任何带有 <see cref="FromKeyedServicesAttribute"/> 属性的参数都将同样从提供给 <see cref="InvokeAsync"/> 的
    ///             <see cref="IServiceProvider"/> 解析，
    ///             而不是从参数 集合解析，并且不会包含在生成的 JSON 模式中。
    ///         </description>
    ///     </item>
    /// </list>
    /// </para>
    /// <para>
    /// 所有其他参数均从 <see cref="CallToolRequestParams.Arguments"/> 字典中的 <see cref="JsonElement"/> 反序列化，
    /// 使用 <see cref="McpServerToolCreateOptions.SerializerOptions"/> 中提供的 <see cref="JsonSerializerOptions"/>；
    /// 如果没有提供，使用 <see cref="McpJsonUtilities.DefaultSettings"/>。
    /// </para>
    /// <para>
    /// 通常，通过 <see cref="CallToolRequestParams.Arguments"/> 字典提供的数据由调用者传递，因此应被视为未经验证且不可信。
    /// 为了向工具调用提供经过验证且可信的数据，请考虑将工具作为实例方法，引用存储在实例中的数据，
    /// 或使用从 <see cref="IServiceProvider"/> 解析的实例或参数向该方法提供数据。
    /// </para>
    /// <para>
    /// 方法的返回值用于创建返回给客户端的 <see cref="CallToolResult"/>：
    /// </para>
    /// <list type="table">
    ///     <item>
    ///         <term><see langword="null"/></term>
    ///         <description>返回一个空的 <see cref="CallToolResult.Content"/> 列表。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="AIContent"/></term>
    ///         <description>使用 <see cref="AIContentExtensions.ToContent(AIContent)"/> 转换为单个 <see cref="ContentBlock"/> 对象。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="string"/></term>
    ///         <description>转换为单个 <see cref="TextContentBlock"/> 对象，其文本设置为字符串值。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="ContentBlock"/></term>
    ///         <description>返回单个项目 <see cref="ContentBlock"/> 列表。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IEnumerable{String}"/> of <see cref="string"/></term>
    ///         <description>每个 <see cref="string"/> 转换为一个 <see cref="TextContentBlock"/> 对象，其文本设置为字符串值。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IEnumerable{AIContent}"/> of <see cref="AIContent"/></term>
    ///         <description>每个 <see cref="AIContent"/> 都会使用 <see cref="AIContentExtensions.ToContent(AIContent)"/> 转换为 <see cref="ContentBlock"/> 对象。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="IEnumerable{ContentBlock}"/> of <see cref="ContentBlock"/></term>
    ///         <description>以 <see cref="ContentBlock"/> 列表形式返回。</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CallToolResult"/></term>
    ///         <description>直接返回，不做任何修改。</description>
    ///     </item>
    ///     <item>
    ///     <term>其他类型</term>
    ///         <description>序列化转换为 JSON 格式，并返回单个 <see cref="ContentBlock"/> 对象，其中 <see cref="ContentBlock.Type"/> 设置为“text”。</description>
    ///     </item>
    /// </list>
    /// </remarks>
    public abstract class McpServerTool : IMcpServerPrimitive
    {
        /// <summary>初始化 <see cref="McpServerTool"/> 类的新实例。</summary>
        protected McpServerTool()
        {
        }

        /// <summary>获取此实例的协议 <see cref="Tool"/> 类型。</summary>
        public abstract Tool ProtocolTool { get; }

        /// <summary>
        /// 获取此工具实例的元数据。
        /// </summary>
        /// <remarks>
        /// 包含来自关联 MethodInfo 和声明类（如果有）的属性，
        /// 类级属性出现在方法级属性之前。
        /// </remarks>
        public abstract IReadOnlyList<object> Metadata { get; }

        /// <summary>调用 <see cref="McpServerTool"/>。</summary>
        /// <param name="request">导致调用此工具的请求信息。</param>
        /// <param name="cancellationToken">
        /// 用于监控取消请求的 <see cref="CancellationToken"/>。
        /// 默认值为 <see cref="CancellationToken.None"/>。
        /// </param>
        /// <returns>调用此工具的响应。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> 为 <see langword="null"/>。</exception>
        public abstract ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 为通过 <see cref="Delegate"/> 实例指定的方法创建 <see cref="McpServerTool"/> 实例。
        /// </summary>
        /// <param name="method">
        /// 通过创建的 <see cref="McpServerTool"/> 表示的方法。
        /// </param>
        /// <param name="options">
        /// 在创建 <see cref="McpServerTool"/> 时使用的可选选项，用于控制其行为。
        /// </param>
        /// <returns>
        /// 用于调用 <paramref name="method"/> 的已创建的 <see cref="McpServerTool"/>。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> 为 <see langword="null"/>。</exception>
        public static McpServerTool Create(
            Delegate method,
            McpServerToolCreateOptions? options = null) =>
            AIFunctionMcpServerTool.Create(method, options);

        /// <summary>
        /// 为通过 <see cref="Delegate"/> 实例指定的方法创建 <see cref="McpServerTool"/> 实例。
        /// </summary>
        /// <param name="method">通过创建的 <see cref="McpServerTool"/> 表示的方法。</param>
        /// <param name="target">如果 <paramref name="method"/> 是实例方法，则为该实例；否则，<see langword="null"/>。</param>
        /// <param name="options">创建 <see cref="McpServerTool"/> 时使用的可选选项，用于控制其行为。</param>
        /// <returns>用于调用 <paramref name="method"/> 而创建的 <see cref="McpServerTool"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> 为 <see langword="null"/>。</exception>
        /// <exception cref="ArgumentException"><paramref name="method"/> 是实例方法，但 <paramref name="target"/> 为 <see langword="null"/>。</exception>
        public static McpServerTool Create(
            MethodInfo method,
            object target = null,
            McpServerToolCreateOptions? options = null) =>
            AIFunctionMcpServerTool.Create(method, target, options);

        /// <summary>
        /// 为某个方法创建一个 <see cref="McpServerTool"/> 实例，该方法通过 <see cref="MethodInfo"/> 指定，
        /// 以及实例方法，同时创建一个 <see cref="Type"/> 实例，该实例表示每次调用该方法时要实例化的目标对象的类型。
        /// </summary>
        /// <param name="method">通过创建的 <see cref="AIFunction"/> 表示的实例方法。</param>
        /// <param name="createTargetFunc">
        /// 每次函数调用时使用的回调，用于创建实例方法 <paramref name="method"/> 所针对类型的实例。
        /// 如果返回的实例是 <see cref="IAsyncDisposable"/> 或 <see cref="IDisposable"/>，则它将在
        /// 方法调用完成后被销毁。
        /// </param>
        /// <param name="options">创建 <see cref="McpServerTool"/> 时使用的可选选项，用于控制其行为。</param>
        /// <returns>已创建用于调用 <paramref name="method"/> 的 <see cref="AIFunction"/>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> 为 <see langword="null"/>。</exception>
        public static McpServerTool Create(
            MethodInfo method,
            Func<RequestContext<CallToolRequestParams>, object> createTargetFunc,
            McpServerToolCreateOptions? options = null) =>
            AIFunctionMcpServerTool.Create(method, createTargetFunc, options);

        /// <summary>创建一个 <see cref="McpServerTool"/> 来包装指定的 <see cref="AIFunction"/>。</summary>
        /// <param name="function">要包装的函数。</param>
        /// <param name="options">在创建 <see cref="McpServerTool"/> 时使用的可选选项，用于控制其行为。</param>
        /// <exception cref="ArgumentNullException"><paramref name="function"/> 为 <see langword="null"/>。</exception>
        /// <remarks>
        /// 与 Create 的其他重载不同，由 <see cref="Create(AIFunction, McpServerToolCreateOptions)"/> 创建的 <see cref="McpServerTool"/>
        /// 不提供针对 MCP 特定概念（例如 <see cref="McpServer"/>）的所有特殊参数处理。
        /// </remarks>
        public static McpServerTool Create(
            AIFunction function,
            McpServerToolCreateOptions? options = null) =>
            AIFunctionMcpServerTool.Create(function, options);

        /// <inheritdoc />
        public override string ToString() => ProtocolTool.Name;

        /// <inheritdoc />
        string IMcpServerPrimitive.Id => ProtocolTool.Name;
    }
}