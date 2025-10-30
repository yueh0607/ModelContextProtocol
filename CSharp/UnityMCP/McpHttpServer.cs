using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnityMCP
{
    /// <summary>
    /// 轻量级HTTP服务器，基于TcpListener实现
    /// 无需外部依赖，完全兼容.NET Standard 2.0
    /// </summary>
    public class McpHttpServer
    {
        private readonly McpServer _mcpServer;
        private TcpListener _listener;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning;
        private readonly int _port;
        private readonly Queue<Action> _mainThreadActions = new Queue<Action>();
        private readonly object _queueLock = new object();

        public event Action<string> OnLog;
        public bool IsRunning => _isRunning;

        public McpHttpServer(McpServer mcpServer, int port = 3000)
        {
            _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
            _port = port;
        }

        /// <summary>
        /// 启动HTTP服务器
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                Log("Server is already running");
                return;
            }

            try
            {
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                Log($"Server started on http://localhost:{_port}");

                // 在后台线程监听连接
                Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Log($"Failed to start server: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止HTTP服务器
        /// </summary>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _listener?.Stop();

            Log("Server stopped");
        }

        /// <summary>
        /// 在Unity主线程更新时调用，处理排队的操作
        /// </summary>
        public void Update()
        {
            lock (_queueLock)
            {
                while (_mainThreadActions.Count > 0)
                {
                    var action = _mainThreadActions.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Log($"Error executing main thread action: {ex.Message}");
                    }
                }
            }
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();

                    // 为每个客户端连接创建处理任务
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    // 服务器已停止
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        Log($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    // 读取HTTP请求
                    var httpRequest = await ReadHttpRequestAsync(stream, cancellationToken);

                    if (httpRequest == null)
                    {
                        return;
                    }

                    // 只处理POST请求
                    if (!httpRequest.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
                    {
                        await SendHttpResponseAsync(stream, 405, "Method Not Allowed", "Only POST is supported");
                        return;
                    }

                    // 处理MCP请求
                    var responseJson = await _mcpServer.HandleRequestAsync(httpRequest.Body);

                    // 发送HTTP响应
                    await SendHttpResponseAsync(stream, 200, "OK", responseJson, "application/json");
                }
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    Log($"Error handling client: {ex.Message}");
                }
            }
        }

        private async Task<HttpRequest> ReadHttpRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            var reader = new StreamReader(stream, Encoding.UTF8);
            var request = new HttpRequest();

            // 读取请求行
            var requestLine = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(requestLine))
            {
                return null;
            }

            var parts = requestLine.Split(' ');
            if (parts.Length < 3)
            {
                return null;
            }

            request.Method = parts[0];
            request.Path = parts[1];

            // 读取头部
            int contentLength = 0;
            string line;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var headerName = line.Substring(0, colonIndex).Trim();
                    var headerValue = line.Substring(colonIndex + 1).Trim();
                    request.Headers[headerName] = headerValue;

                    if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(headerValue, out contentLength);
                    }
                }
            }

            // 读取body
            if (contentLength > 0)
            {
                var buffer = new char[contentLength];
                var totalRead = 0;
                while (totalRead < contentLength)
                {
                    var read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                request.Body = new string(buffer, 0, totalRead);
            }

            return request;
        }

        private async Task SendHttpResponseAsync(NetworkStream stream, int statusCode, string statusText,
            string body, string contentType = "text/plain")
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var response = new StringBuilder();

            response.AppendLine($"HTTP/1.1 {statusCode} {statusText}");
            response.AppendLine($"Content-Type: {contentType}; charset=utf-8");
            response.AppendLine($"Content-Length: {bodyBytes.Length}");
            response.AppendLine("Access-Control-Allow-Origin: *");
            response.AppendLine("Connection: close");
            response.AppendLine();

            var headerBytes = Encoding.UTF8.GetBytes(response.ToString());

            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            await stream.FlushAsync();
        }

        private void Log(string message)
        {
            OnLog?.Invoke($"[McpHttpServer] {message}");
        }

        private class HttpRequest
        {
            public string Method { get; set; }
            public string Path { get; set; }
            public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
            public string Body { get; set; }
        }
    }
}
