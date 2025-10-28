using System;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Client
{
    /// <summary>
    /// 提供用于创建 <see cref="McpClient"/> 实例的配置选项。
    /// </summary>
    /// <remarks>
    /// 这些选项通常在创建客户端时传递给 <see cref="McpClient.CreateAsync"/>。
    /// 它们定义客户端功能、协议版本和其他客户端特定设置。
    /// </remarks>
    public sealed class McpClientOptions
    {
        private McpClientHandlers _handlers;

        /// <summary>
        /// 获取或设置此客户端实现的相关信息，包括其名称和版本。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此信息在初始化期间发送到服务器，用于识别客户端。
        /// 它通常显示在服务器日志中，可用于调试和兼容性检查。
        /// </para>
        /// <para>
        /// 未指定时，将使用来自当前进程的信息。
        /// </para>
        /// </remarks>
        public Implementation ClientInfo { get; set; }

        /// <summary>
        /// 获取或设置要通告给服务器的客户端功能。
        /// </summary>
        public ClientCapabilities Capabilities { get; set; }

        /// <summary>
        /// 使用基于日期的版本控制方案，获取或设置要从服务器请求的协议版本。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 协议版本是初始化握手的关键部分。客户端和服务器必须就兼容的协议版本达成一致才能成功通信。
        /// </para>
        /// <para>
        /// 如果非 <see langword="null"/>，则此版本将发送到服务器，
        /// 并且如果服务器响应中的版本与此版本不匹配，则握手将失败。
        /// 如果为 <see langword="null"/>，则客户端将请求服务器支持的最新版本，
        /// 但允许服务器在其响应中通告的任何受支持版本。
        /// </para>
        /// </remarks>
        public string ProtocolVersion { get; set; }

        /// <summary>
        /// 获取或设置客户端-服务器初始化握手序列的超时时间。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此超时时间决定了客户端在初始化协议握手期间等待服务器响应的时间。
        /// 如果服务器在此时间范围内未响应，将抛出异常。
        /// </para>
        /// <para>
        /// 设置适当的超时时间可防止客户端在连接到无响应的服务器时无限期挂起。
        /// </para>
        /// <para>默认值为 60 秒。</para>
        /// </remarks>
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// 获取或设置客户端用于处理协议消息的处理程序容器。
        /// </summary>
        public McpClientHandlers Handlers 
        { 
            get => _handlers ?? (_handlers = new McpClientHandlers());
            set => _handlers = value ?? throw new ArgumentNullException("value");
        }
    }
}