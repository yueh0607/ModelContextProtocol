using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Client
{
    /// <summary>
    /// 提供一个容器，用于存放创建 MCP 客户端时使用的处理程序。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此类提供了一个集中式委托集合，用于实现模型上下文协议的各种功能。
    /// </para>
    /// <para>
    /// 此类中的每个处理程序都对应于模型上下文协议中的一个特定客户端端点，并且负责处理特定类型的消息。
    /// 这些处理程序用于自定义MCP 服务器的行为，方法是提供各种协议操作的实现。
    /// </para>
    /// <para>
    /// 当服务器向客户端发送消息时，系统会根据协议规范调用相应的处理程序来处理该消息。
    /// 根据序号、区分大小写的字符串比较结果来选择处理哪个处理程序。
    /// </para>
    /// </remarks>
    public class McpClientHandlers
    {
        /// <summary>获取或设置要向客户端注册的通知处理程序。</summary>
        /// <remarks>
        /// <para>
        /// 构造后，客户端将枚举这些处理程序一次，每个通知方法键可能包含多个处理程序。
        /// 初始化后，客户端将不会重新枚举该序列。
        /// </para>
        /// <para>
        /// 通知处理程序允许客户端响应服务器发送的特定方法通知。
        /// 集合中的每个键都是一个通知方法名称，每个值都是一个回调函数，当收到包含该方法的通知时，将调用该回调函数。
        /// </para>
        /// <para>
        /// 通过 <see cref="NotificationHandlers"/> 提供的处理程序将在客户端的整个生命周期内向客户端注册。
        /// 对于瞬态处理程序，可以使用 <see cref="IMcpEndpoint.RegisterNotificationHandler"/> 来注册一个处理程序，
        /// 该处理程序之后可以通过处理从该方法返回的 <see cref="IAsyncDisposable"/> 来取消注册。
        /// </para>
        /// </remarks>
        public IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>>
            NotificationHandlers { get; set; }

        /// <summary>
        /// 获取或设置 <see cref="RequestMethods.RootsList"/> 请求的处理程序。
        /// </summary>
        /// <remarks>
        /// 当客户端发送 <see cref="RequestMethods.RootsList"/> 请求以检索可用根时，将调用此处理程序。
        /// 该处理程序接收请求参数，并应返回包含可用根集合的 <see cref="ListRootsResult"/>。
        /// </remarks>
        public Func<ListRootsRequestParams, CancellationToken, ValueTask<ListRootsResult>> RootsHandler { get; set; }

        /// <summary>
        /// 获取或设置用于处理 <see cref="RequestMethods.ElicitationCreate"/> 请求的处理程序。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当 MCP 服务器请求客户端在交互过程中提供额外信息时，将调用此处理程序函数。
        /// </para>
        /// <para>
        /// 该处理程序接收消息参数和取消令牌。
        /// 它应该返回一个包含对引出请求的响应的 <see cref="ElicitResult"/>。
        /// </para>
        /// </remarks>
        public Func<ElicitRequestParams, CancellationToken, ValueTask<ElicitResult>> ElicitationHandler { get; set; }

        /// <summary>
        /// 获取或设置用于处理 <see cref="RequestMethods.SamplingCreateMessage"/> 请求的处理程序。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 当 MCP 服务器请求客户端使用 AI 模型生成内容时，会调用此处理程序函数。
        /// 客户端必须设置此属性才能使采样功能正常工作。
        /// </para>
        /// <para>
        /// 该处理程序接收消息参数、更新进度报告器和取消令牌。
        /// 它应该返回一个包含生成内容的 <see cref="CreateMessageResult"/>。
        /// </para>
        /// <para>
        /// 您可以使用 <see cref="McpClientExtensions.CreateSamplingHandler"/> 扩展创建处理程序。
        /// 任何 <see cref="IChatClient"/> 实现的方法。
        /// </para>
        /// </remarks>
        public Func<CreateMessageRequestParams, IProgress<ProgressNotificationValue>, CancellationToken,
            ValueTask<CreateMessageResult>> SamplingHandler { get; set; }
    }
}