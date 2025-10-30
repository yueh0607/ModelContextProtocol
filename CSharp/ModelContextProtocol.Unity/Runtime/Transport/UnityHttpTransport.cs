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
    /// 基于 HTTP POST 的 IMcpTransport 实现（仅用于 Unity 本地测试）。
    /// - 每个请求：单条 JSON-RPC 消息（请求体为 JSON），响应：单条 JSON（Response/Error）。
    /// - 仅支持 POST /，Content-Type: application/json。
    /// - 单请求-单响应，短连接（响应后关闭）。
    /// - 简化实现：一次只处理一个挂起请求（足够用于 Editor 环境联调）。
    /// </summary>
    public sealed class UnityHttpTransport : IMcpTransport, IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private TcpClient _currentClient;
        private NetworkStream _currentStream;
        private StreamWriter _currentWriter;
        private string _pendingRequestJson; // 挂起的请求 JSON（供 ReadMessageAsync 返回）
        private readonly SemaphoreSlim _acceptLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private volatile bool _stopped;

        public UnityHttpTransport(int port = 8767)
        {
            _port = port;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopped = false;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            return Task.CompletedTask;
        }

        public async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            if (_stopped)
                return null;

            await _acceptLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 接受一个客户端
                _currentClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                _currentStream = _currentClient.GetStream();
                var reader = new StreamReader(_currentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
                _currentWriter = new StreamWriter(_currentStream, new UTF8Encoding(false), 4096, leaveOpen: true)
                {
                    NewLine = "\r\n",
                    AutoFlush = true
                };

                // 读取 HTTP 请求行
                var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(requestLine) || !requestLine.StartsWith("POST ", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteHttpResponseAsync(statusCode: 405, statusText: "Method Not Allowed", body: "Only POST is supported").ConfigureAwait(false);
                    CleanupConnection();
                    return null;
                }

                // 读取请求头
                int contentLength = 0;
                string contentType = null;
                string line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    int idx = line.IndexOf(':');
                    if (idx > 0)
                    {
                        string name = line.Substring(0, idx).Trim();
                        string value = line.Substring(idx + 1).Trim();
                        if (name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(value, out contentLength);
                        }
                        else if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            contentType = value;
                        }
                    }
                }

                if (contentLength <= 0)
                {
                    await WriteHttpResponseAsync(400, "Bad Request", "Missing Content-Length").ConfigureAwait(false);
                    CleanupConnection();
                    return null;
                }

                // 读取请求体
                char[] buf = new char[contentLength];
                int read = 0;
                while (read < contentLength)
                {
                    int r = await reader.ReadAsync(buf, read, contentLength - read).ConfigureAwait(false);
                    if (r <= 0) break;
                    read += r;
                }

                _pendingRequestJson = new string(buf, 0, read);
                return _pendingRequestJson;
            }
            catch
            {
                CleanupConnection();
                return null;
            }
        }

        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (_stopped || _currentWriter == null)
                return;

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 写 HTTP 响应并关闭连接
                await WriteHttpResponseAsync(200, "OK", message, "application/json").ConfigureAwait(false);
            }
            finally
            {
                CleanupConnection();
                _writeLock.Release();
                _acceptLock.Release(); // 允许下一次请求
            }
        }

        private async Task WriteHttpResponseAsync(int statusCode, string statusText, string body, string contentType = "text/plain")
        {
            if (_currentWriter == null) return;

            byte[] bodyBytes = Encoding.UTF8.GetBytes(body ?? string.Empty);

            await _currentWriter.WriteLineAsync($"HTTP/1.1 {statusCode} {statusText}").ConfigureAwait(false);
            await _currentWriter.WriteLineAsync($"Content-Type: {contentType}; charset=utf-8").ConfigureAwait(false);
            await _currentWriter.WriteLineAsync($"Content-Length: {bodyBytes.Length}").ConfigureAwait(false);
            await _currentWriter.WriteLineAsync("Connection: close").ConfigureAwait(false);
            await _currentWriter.WriteLineAsync().ConfigureAwait(false);
            await _currentWriter.FlushAsync().ConfigureAwait(false);

            // 写入 body 原始字节
            if (_currentStream != null && bodyBytes.Length > 0)
            {
                await _currentStream.WriteAsync(bodyBytes, 0, bodyBytes.Length).ConfigureAwait(false);
                await _currentStream.FlushAsync().ConfigureAwait(false);
            }
        }

        private void CleanupConnection()
        {
            try { _currentWriter?.Dispose(); } catch { }
            try { _currentStream?.Dispose(); } catch { }
            try { _currentClient?.Close(); } catch { }
            _currentWriter = null;
            _currentStream = null;
            _currentClient = null;
            _pendingRequestJson = null;
        }

        public void Stop()
        {
            _stopped = true;
            CleanupConnection();
            try { _listener?.Stop(); } catch { }
        }

        public void Dispose()
        {
            Stop();
            _writeLock.Dispose();
            _acceptLock.Dispose();
        }
    }
}


