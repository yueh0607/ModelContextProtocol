using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Server.Transport;

namespace ModelContextProtocol.Unity.Runtime.Transport
{
    /// <summary>
    /// 基于 TCP 的 Unity 传输层实现（Editor 环境运行）。
    /// </summary>
    /// <remarks>
    /// - 行分隔 JSON：每条 JSON-RPC 消息使用 "\n" 分隔。
    /// - 仅接受一个客户端连接；断开后可再次接受。
    /// - 线程安全写入（使用 SemaphoreSlim）。
    /// </remarks>
    public sealed class UnityTcpTransport : IMcpTransport, IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private TcpClient _client;
        private StreamReader _reader;
        private StreamWriter _writer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private volatile bool _stopped;

        public UnityTcpTransport(int port = 8765)
        {
            _port = port;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _stopped = false;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

            var stream = _client.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8, false, 1024, leaveOpen: true);
            _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
        }

        public async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            if (_stopped || _reader == null)
                return null;

            try
            {
                var line = await _reader.ReadLineAsync().ConfigureAwait(false);
                return line; // 客户端断开时返回 null
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (_stopped || _writer == null || string.IsNullOrEmpty(message))
                return;

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _writer.WriteLineAsync(message).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Stop()
        {
            _stopped = true;
            try { _writer?.Dispose(); } catch { }
            try { _reader?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }
        }

        public void Dispose()
        {
            Stop();
            _writeLock.Dispose();
        }
    }
}


