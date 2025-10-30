using System.Threading;
using System.Threading.Tasks;

namespace MapleModelContextProtocol.Server.Transport
{
    /// <summary>
    /// 定义 MCP 服务器的传输层接口。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 传输层负责在客户端和服务器之间传输 JSON-RPC 消息。
    /// 不同的传输实现可以支持不同的通信方式（stdio、WebSocket、HTTP等）。
    /// </para>
    /// <para>
    /// 此接口设计为异步和流式传输，适合 Unity 环境使用。
    /// </para>
    /// </remarks>
    public interface IMcpTransport
    {
        /// <summary>
        /// 从传输层读取一条完整的 JSON-RPC 消息。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>读取到的 JSON 字符串消息，如果连接关闭则返回 <see langword="null"/>。</returns>
        Task<string> ReadMessageAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 向传输层写入一条 JSON-RPC 消息。
        /// </summary>
        /// <param name="message">要发送的 JSON 字符串消息。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task WriteMessageAsync(string message, CancellationToken cancellationToken);

        /// <summary>
        /// 启动传输层，开始监听消息。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 停止传输层，关闭连接。
        /// </summary>
        void Stop();
    }
}

