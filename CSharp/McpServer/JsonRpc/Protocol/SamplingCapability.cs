using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Client;
using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示客户端使用 AI 模型生成文本或其他内容的能力。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此功能使 MCP 客户端能够响应来自 MCP 服务器的采样请求。
    /// </para>
    /// <para>
    /// 启用此功能后，MCP 服务器可以请求客户端使用 AI 模型生成内容。
    /// 客户端必须设置 <see cref="JsonRpc.Client.McpClientHandlers.SamplingHandler"/> 来处理这些请求。
    /// </para>
    /// <para>
    /// 此类特意留空，因为模型上下文协议规范目前尚未定义用于采样功能的附加属性。
    /// 该规范的未来版本可能会通过其他配置选项扩展此功能。
    /// </para>
    /// </remarks>
    public sealed class SamplingCapability
    {
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
        /// 您可以使用 <see cref="McpClient.CreateSamplingHandler"/> 扩展创建处理程序。
        /// 任何 <see cref="IChatClient"/> 实现的方法。
        /// </para>
        /// </remarks>
        [JsonIgnore]
        [Obsolete("Use McpClientOptions.Handlers.SamplingHandler instead. This member will be removed in a subsequent release.")] // See: https://github.com/modelcontextprotocol/csharp-sdk/issues/774
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Func<CreateMessageRequestParams, IProgress<ProgressNotificationValue>, CancellationToken, ValueTask<CreateMessageResult>> SamplingHandler { get; set; }
    }
}