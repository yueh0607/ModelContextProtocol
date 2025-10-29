using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapleModelContextProtocol.Server.Transport
{
    /// <summary>
    /// 基于标准输入/输出流的 MCP 传输层实现。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 此实现使用 stdin 接收消息，使用 stdout 发送消息。
    /// 每条消息以换行符（\n）分隔，遵循 JSON-RPC over stdio 规范。
    /// </para>
    /// <para>
    /// 此实现适用于 Unity 命令行环境、编辑器扩展或任何通过标准流通信的场景。
    /// </para>
    /// </remarks>
    public sealed class StdioTransport : IMcpTransport
    {
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly StringBuilder _buffer;
        private readonly SemaphoreSlim _writeLock;
        private bool _disposed;

        /// <summary>
        /// 使用默认的 stdin 和 stdout 初始化新的 StdioTransport 实例。
        /// </summary>
        public StdioTransport()
            : this(Console.In, Console.Out)
        {
        }

        /// <summary>
        /// 使用指定的输入和输出流初始化新的 StdioTransport 实例。
        /// </summary>
        /// <param name="input">用于读取消息的文本读取器。</param>
        /// <param name="output">用于写入消息的文本写入器。</param>
        public StdioTransport(TextReader input, TextWriter output)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _buffer = new StringBuilder();
            _writeLock = new SemaphoreSlim(1, 1);
            _disposed = false;
        }

        /// <inheritdoc />
        public Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            return ReadMessageInternalAsync(cancellationToken);
        }

        private async Task<string> ReadMessageInternalAsync(CancellationToken cancellationToken)
        {
            _buffer.Clear();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 异步读取一个字符
                var buffer = new char[1];
                var charsRead = await _input.ReadAsync(buffer, 0, 1);

                if (charsRead == 0)
                {
                    // 流已关闭
                    return null;
                }

                char ch = buffer[0];

                // JSON-RPC over stdio 使用换行符分隔消息
                if (ch == '\n')
                {
                    string message = _buffer.ToString();
                    _buffer.Clear();
                    return message;
                }

                // 跳过回车符（支持 \r\n）
                if (ch != '\r')
                {
                    _buffer.Append(ch);
                }
            }
        }

        /// <inheritdoc />
        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StdioTransport));

            if (string.IsNullOrEmpty(message))
                return;

            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                await _output.WriteAsync(message);
                await _output.WriteAsync('\n');
                await _output.FlushAsync();
            }
            finally
            {
                _writeLock.Release();
            }
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // stdio 传输在构造时就已经准备好了，无需额外启动步骤
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _disposed = true;
            // 注意：我们不关闭 Console.In/Console.Out，因为它们是全局的
            // 如果是自定义的流，调用者应该负责关闭
        }
    }
}

