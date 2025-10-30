using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Server.Transport;
using UnityEngine;

namespace ModelContextProtocol.Unity.Runtime.Transport
{
    /// <summary>
    /// 修复版：基于 HTTP POST 的 IMcpTransport 实现
    /// 修复了异步响应时连接过早关闭的问题
    /// </summary>
    public sealed class UnityHttpTransportFixed : IMcpTransport, IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private TcpClient _currentClient;
        private NetworkStream _currentStream;
        private StreamWriter _currentWriter;
        private readonly SemaphoreSlim _readLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> _writeCompletedTcs;
        private volatile bool _stopped;

        public UnityHttpTransportFixed(int port = 8767)
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

            await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // 接受新连接前，确保上一个连接已完全处理
                if (_writeCompletedTcs != null)
                {
                    await _writeCompletedTcs.Task.ConfigureAwait(false);
                }

                // 清理上一个连接
                CleanupConnection();

                // 接受新客户端
                _currentClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
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
                if (string.IsNullOrEmpty(requestLine) || !requestLine.StartsWith("POST ", StringComparison.OrdinalIgnoreCase))
                {
                    await WriteHttpResponseAsync(405, "Method Not Allowed", "Only POST is supported").ConfigureAwait(false);
                    _writeCompletedTcs?.TrySetResult(true);
                    return null;
                }

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
                return new string(buf, 0, read);
            }
            catch (Exception)
            {
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
                try { UnityEngine.Debug.Log($"[MCP][HTTP write] body length={(message?.Length ?? -1)}"); } catch { }
                // 写 HTTP 响应
                await WriteHttpResponseAsync(200, "OK", message, "application/json").ConfigureAwait(false);
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
        }

        public void Dispose()
        {
            Stop();
            _readLock.Dispose();
            _writeLock.Dispose();
        }
    }
}

