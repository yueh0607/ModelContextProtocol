using System;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Protocol;

namespace JsonRpc.Server
{
    public abstract partial class McpServer
    {
        /// <summary>
        /// 获取客户端支持的功能。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 这些功能在初始化握手期间建立，并指示客户端支持哪些功能，
        /// 例如采样、根和其他特定于协议的功能。
        /// </para>
        /// <para>
        /// 服务器实现可以检查这些功能，以确定哪些功能在与客户端交互时可用。
        /// </para>
        /// </remarks>
        public abstract ClientCapabilities ClientCapabilities { get; }

        /// <summary>
        /// 获取已连接客户端的版本和实现信息。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此属性包含已连接到此服务器的客户端的标识信息，包括其名称和版本。
        /// 此信息由客户端在初始化期间提供。
        /// </para>
        /// <para>
        /// 服务器实现可以使用此信息进行日志记录、跟踪客户端版本，或实现客户端特定的行为。
        /// </para>
        /// </remarks>
        public abstract Implementation ClientInfo { get; }

        /// <summary>
        /// 获取用于构建此服务器的选项。
        /// </summary>
        /// <remarks>
        /// 这些选项定义了服务器的功能、协议版本以及用于初始化服务器的其他配置设置。
        /// </remarks>
        public abstract McpServerOptions ServerOptions { get; }

        /// <summary>
        /// 获取服务器的服务提供者。
        /// </summary>
        public abstract IServiceProvider Services { get; }

        /// <summary>
        /// 获取客户端设置的最后一个日志记录级别，如果从未设置，则获取 <see langword="null"/>。
        /// </summary>
        public abstract LoggingLevel? LoggingLevel { get; }

        /// <summary>
        /// 运行服务器，监听并处理客户端请求。
        /// </summary>
        public abstract Task RunAsync(CancellationToken cancellationToken = default);
    }
}