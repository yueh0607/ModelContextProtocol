using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MapleModelContextProtocol.Server.Transport
{
    /// <summary>
    /// 基于 HTTP POST 的 IMcpTransport 实现（适用于控制台程序）
    /// 每个 HTTP POST 请求-响应对应一次 JSON-RPC 消息交换
    /// </summary>
    public sealed class HttpTransport : IMcpTransport, IDisposable
    {
        private readonly int _port;
        private readonly string _path;
        private TcpListener _listener;
        private TcpClient _currentClient;
        private NetworkStream _currentStream;
        private StreamWriter _currentWriter;
        private readonly SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> _writeCompletedTcs;
        private volatile bool _stopped;
        private Action<string> _logger;

        /// <summary>
        /// 创建一个新的 HTTP 传输实例
        /// </summary>
        /// <param name="port">监听端口（默认 8767）</param>
        /// <param name="path">HTTP 路径（默认 "/"）</param>
        /// <param name="logger">可选的日志记录器</param>
        public HttpTransport(int port = 8767, string path = "/", Action<string> logger = null)
        {
            _port = port;
            _path = path ?? "/";
            _logger = logger;
        }

        private void Log(string message)
        {
            _logger?.Invoke($"[HttpTransport] {message}");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _stopped = false;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            Log($"HTTP Server started on http://localhost:{_port}{_path}");
            return Task.CompletedTask;
        }

        public async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            if (_stopped)
                return null;

            await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 确保上一个连接已完全处理
                if (_writeCompletedTcs != null)
                {
                    await _writeCompletedTcs.Task.ConfigureAwait(false);
                }

                // 清理上一个连接
                CleanupConnection();

                // 接受新客户端
                Log("Waiting for HTTP client connection...");
                _currentClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                Log("Client connected");
                
                _currentStream = _currentClient.GetStream();
                var reader = new StreamReader(_currentStream, Encoding.UTF8, false, 4096, true);
                _currentWriter = new StreamWriter(_currentStream, new UTF8Encoding(false), 4096, true)
                {
                    NewLine = "\r\n",
                    AutoFlush = true
                };

                // 创建新的写完成信号
                _writeCompletedTcs = new TaskCompletionSource<bool>();

                // 读取 HTTP 请求行
                var requestLine = await reader.ReadLineAsync().ConfigureAwait(false);
                Log($"Request line: {requestLine}");
                
                if (string.IsNullOrEmpty(requestLine))
                {
                    await WriteHttpResponseAsync(400, "Bad Request", "Empty request").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
                    return null;
                }

                // 解析请求行 (GET/POST /path HTTP/1.1)
                var parts = requestLine.Split(' ');
                if (parts.Length < 2)
                {
                    await WriteHttpResponseAsync(400, "Bad Request", "Invalid request line").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
                    return null;
                }

                string method = parts[0].ToUpperInvariant();
                
                // 读取请求头
                int contentLength = 0;
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
                    }
                }

                // 处理 GET 请求（返回服务器信息）
                if (method == "GET")
                {
                    string info = "MCP Server HTTP Transport\n\nUse POST request with JSON-RPC message in body.";
                    await WriteHttpResponseAsync(200, "OK", info, "text/plain").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
                    return null;
                }

                // 只接受 POST 请求
                if (method != "POST")
                {
                    await WriteHttpResponseAsync(405, "Method Not Allowed", "Only POST is supported").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
                    return null;
                }

                if (contentLength <= 0)
                {
                    await WriteHttpResponseAsync(400, "Bad Request", "Missing Content-Length").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
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
                
                string body = new string(buf, 0, read);
                Log($"Received message: {body}");
                return body;
            }
            catch (Exception ex)
            {
                Log($"Error reading message: {ex.Message}");
                _writeCompletedTcs?.TrySetResult(true);
                CleanupConnection();
                return null;
            }
            finally
            {
                _readLock.Release();
            }
        }

        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (_stopped || _currentWriter == null)
            {
                _writeCompletedTcs?.TrySetResult(true);
                return;
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 对于通知确认（空对象 "{}"），发送一个简单的 HTTP 200 OK
                if (string.IsNullOrEmpty(message) || message.Trim() == "{}")
                {
                    Log("Sending HTTP 200 OK acknowledgment (no JSON-RPC response)");
                    await WriteHttpResponseAsync(200, "OK", "", "application/json").ConfigureAwait(false);
                }
                else
                {
                    Log($"Sending response: {message}");
                    // 写 HTTP 响应
                    await WriteHttpResponseAsync(200, "OK", message, "application/json").ConfigureAwait(false);
                }
            }
            finally
            {
                // 响应已发送，标记写完成
                _writeCompletedTcs?.TrySetResult(true);
                _writeLock.Release();
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
        }

        public void Stop()
        {
            _stopped = true;
            _writeCompletedTcs?.TrySetResult(true);
            CleanupConnection();
            try { _listener?.Stop(); } catch { }
            Log("HTTP Server stopped");
        }

        public void Dispose()
        {
            Stop();
            _readLock.Dispose();
            _writeLock.Dispose();
        }
    }
}

