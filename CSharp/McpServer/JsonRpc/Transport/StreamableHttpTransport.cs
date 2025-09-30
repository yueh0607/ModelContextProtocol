using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;
using Newtonsoft.Json;
using McpServerLib.Utils;
using System.Collections.Generic;

namespace JsonRpc.Transport
{
    public class StreamableHttpTransport : IJsonRpcTransport
    {
        private readonly HttpListener _httpListener;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, HttpListenerResponse> _activeConnections = new ConcurrentDictionary<string, HttpListenerResponse>();
        private bool _disposed = false;
        private Task _serverTask;

        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public bool IsConnected => !_disposed && _httpListener?.IsListening == true;

        public StreamableHttpTransport(string serverUrl, HttpClient httpClient = null, JsonSerializerSettings serializerSettings = null)
        {
            McpLogger.Debug("正在创建 StreamableHttpTransport，URL: {0}", serverUrl ?? "null");

            if (string.IsNullOrEmpty(serverUrl))
                serverUrl = "http://localhost:3000/mcp/";

            if (!serverUrl.EndsWith("/"))
                serverUrl += "/";

            McpLogger.Debug("最终使用的 URL: {0}", serverUrl);

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(serverUrl);

            McpLogger.Debug("HttpListener 已创建并添加前缀");

            _serializerSettings = serializerSettings ?? new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.None
            };
        }

        public async Task SendAsync(object message, CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                McpLogger.Error("尝试在已释放的传输上发送消息");
                throw new ObjectDisposedException(nameof(StreamableHttpTransport));
            }

            try
            {
                var json = JsonConvert.SerializeObject(message, _serializerSettings);
                McpLogger.Debug("准备发送消息到 {0} 个活跃连接: {1}", _activeConnections.Count, json);

                // 检查是否是JSON-RPC响应
                var isJsonRpcResponse = message is JsonRpcResponse;
                McpLogger.Debug("消息类型: {0}, 是否为JSON-RPC响应: {1}", message.GetType().Name, isJsonRpcResponse);

                // 发送 Server-Sent Event 到所有活跃连接
                var eventData = $"data: {json}\n\n";
                var eventBytes = Encoding.UTF8.GetBytes(eventData);

                var sentCount = 0;
                var connectionsToClose = new List<string>();

                foreach (var kvp in _activeConnections)
                {
                    var connectionId = kvp.Key;
                    var connection = kvp.Value;

                    try
                    {
                        if (connection.OutputStream.CanWrite)
                        {
                            await connection.OutputStream.WriteAsync(eventBytes, 0, eventBytes.Length, cancellationToken);
                            await connection.OutputStream.FlushAsync(cancellationToken);
                            sentCount++;
                            McpLogger.Debug("消息已发送到连接 [{0}]", connectionId);

                            // 如果这是JSON-RPC响应，标记连接待关闭
                            if (isJsonRpcResponse)
                            {
                                connectionsToClose.Add(connectionId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        McpLogger.Error("发送消息到连接 [{0}] 时发生错误", ex);
                        connectionsToClose.Add(connectionId); // 出错的连接也要清理
                        OnErrorOccurred(ex);
                    }
                }

                // 关闭已完成的连接（根据MCP规范，响应发送后应关闭流）
                foreach (var connectionId in connectionsToClose)
                {
                    if (_activeConnections.TryRemove(connectionId, out var connection))
                    {
                        try
                        {
                            connection.Close();
                            McpLogger.Debug("JSON-RPC响应发送完成，SSE连接已关闭 [{0}]", connectionId);
                        }
                        catch (Exception ex)
                        {
                            McpLogger.Warning(string.Format("关闭连接时发生错误 [{0}]: {1}", connectionId, ex.Message));
                        }
                    }
                }

                McpLogger.Debug("消息已发送到 {0} 个连接，关闭了 {1} 个连接", sentCount, connectionsToClose.Count);
            }
            catch (Exception ex)
            {
                McpLogger.Error("发送消息时发生错误", ex);
                OnErrorOccurred(ex);
                throw;
            }
        }

        public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                McpLogger.Error("尝试启动已释放的 StreamableHttpTransport");
                throw new ObjectDisposedException(nameof(StreamableHttpTransport));
            }

            try
            {
                McpLogger.Debug("正在启动 HttpListener...");
                _httpListener.Start();
                McpLogger.Info(string.Format("HTTP 服务器已启动，监听地址: {0}", string.Join(", ", _httpListener.Prefixes)));
                Console.Error.WriteLine($"HTTP 服务器已启动，监听地址: {string.Join(", ", _httpListener.Prefixes)}");

                var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
                McpLogger.Debug("已创建组合取消令牌，开始服务器循环");
                _serverTask = Task.Run(async () => await ServerLoop(combinedCts.Token), combinedCts.Token);

                await _serverTask;
                McpLogger.Debug("服务器任务已完成");
            }
            catch (Exception ex)
            {
                McpLogger.Error("启动 StreamableHttpTransport 时发生错误", ex);
                OnErrorOccurred(ex);
                throw;
            }
        }

        private async Task ServerLoop(CancellationToken cancellationToken)
        {
            McpLogger.Debug("服务器循环已开始");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    McpLogger.Debug("等待 HTTP 请求...");
                    var context = await _httpListener.GetContextAsync();
                    McpLogger.Debug("收到 HTTP 请求: {0} {1}", context.Request.HttpMethod, context.Request.Url.AbsolutePath);

                    // 处理请求在后台运行，不阻塞主循环
                    _ = Task.Run(async () => await ProcessRequest(context, cancellationToken), cancellationToken);
                }
                catch (ObjectDisposedException)
                {
                    McpLogger.Debug("HttpListener 已释放，退出服务器循环");
                    break;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    McpLogger.Error("服务器循环中发生错误", ex);
                    OnErrorOccurred(ex);
                }
            }
            McpLogger.Debug("服务器循环已结束");
        }

        private async Task ProcessRequest(HttpListenerContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            var response = context.Response;
            var connectionId = Guid.NewGuid().ToString();

            McpLogger.Debug("处理请求 [{0}]: {1} {2} 来自 {3}", connectionId, request.HttpMethod, request.Url.AbsolutePath, request.RemoteEndPoint);

            try
            {
                // 设置 CORS 头
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    McpLogger.Debug("处理 OPTIONS 请求 [{0}]", connectionId);
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url.AbsolutePath.ToLowerInvariant();
                McpLogger.Debug("请求路径 [{0}]: {1}", connectionId, path);

                if (path.EndsWith("/events") && request.HttpMethod == "GET")
                {
                    McpLogger.Debug("处理 SSE 连接 [{0}]", connectionId);
                    await HandleEventStream(response, connectionId, cancellationToken);
                }
                else if (request.HttpMethod == "POST")
                {
                    McpLogger.Debug("处理 JSON-RPC POST 请求 [{0}]", connectionId);
                    await HandleJsonRpcRequest(request, response, cancellationToken);
                }
                else if (request.HttpMethod == "GET" && (path.EndsWith("/mcp/") || path.EndsWith("/mcp")))
                {
                    McpLogger.Debug("处理根路径GET请求，返回端点信息 [{0}]", connectionId);
                    response.StatusCode = 200;
                    response.ContentType = "application/json";

                    var endpointInfo = "{\"sse_endpoint\":\"/mcp/events\",\"rpc_endpoint\":\"/mcp/\"}";
                    var endpointBytes = Encoding.UTF8.GetBytes(endpointInfo);
                    response.ContentLength64 = endpointBytes.Length;
                    await response.OutputStream.WriteAsync(endpointBytes, 0, endpointBytes.Length, cancellationToken);
                    response.Close();
                }
                else
                {
                    McpLogger.Warning(string.Format("未知请求路径 [{0}]: {1} {2}", connectionId, request.HttpMethod, path));
                    McpLogger.Warning("可用端点: POST /mcp/ (JSON-RPC), GET /mcp/events (SSE), GET /mcp/ (端点信息)");
                    response.StatusCode = 404;
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                McpLogger.Error("处理请求时发生错误 [{0}]", ex);
                OnErrorOccurred(ex);
                try
                {
                    response.StatusCode = 500;
                    response.Close();
                }
                catch
                {
                    // 忽略关闭时的错误
                }
            }
            finally
            {
                _activeConnections.TryRemove(connectionId, out _);
                McpLogger.Debug("请求处理完成 [{0}]", connectionId);
            }
        }

        private async Task HandleJsonRpcRequest(HttpListenerRequest request, HttpListenerResponse response, CancellationToken cancellationToken)
        {
            McpLogger.Debug("开始处理 JSON-RPC 请求");
            string jsonRequest;
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                jsonRequest = await reader.ReadToEndAsync();
                McpLogger.Debug("收到 JSON-RPC 消息: {0}", jsonRequest);
            }

            if (string.IsNullOrEmpty(jsonRequest))
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            // 检查Accept头以确定响应类型
            var acceptHeader = request.Headers["Accept"] ?? "";
            var supportsSSE = acceptHeader.Contains("text/event-stream");
            var supportsJSON = acceptHeader.Contains("application/json");

            McpLogger.Debug("Accept头: {0}, 支持SSE: {1}, 支持JSON: {2}", acceptHeader, supportsSSE, supportsJSON);

            // 解析JSON-RPC消息以确定类型
            JsonRpcRequest rpcRequest = null;
            try
            {
                rpcRequest = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonRpcRequest>(jsonRequest);
            }
            catch (Exception ex)
            {
                McpLogger.Error("解析JSON-RPC请求失败", ex);
                response.StatusCode = 400;
                response.Close();
                return;
            }

            // 根据MCP规范处理不同类型的消息
            if (rpcRequest.Id == null)
            {
                // 这是一个通知或响应，返回202 Accepted
                OnMessageReceived(new MessageReceivedEventArgs(jsonRequest, typeof(object)));
                response.StatusCode = 202;
                response.Close();
                McpLogger.Debug("处理通知/响应，返回 202 Accepted");
            }
            else
            {
                // 这是一个请求，根据Accept头决定响应方式
                if (supportsSSE)
                {
                    // 启动SSE流并触发消息处理
                    var connectionId = Guid.NewGuid().ToString();
                    McpLogger.Debug("启动SSE流处理请求 [{0}]", connectionId);

                    response.ContentType = "text/event-stream";
                    response.Headers.Add("Cache-Control", "no-cache");
                    response.Headers.Add("Connection", "keep-alive");
                    response.StatusCode = 200;

                    _activeConnections[connectionId] = response;
                    McpLogger.Debug("SSE连接已添加 [{0}]，当前连接数: {1}", connectionId, _activeConnections.Count);

                    // 触发消息处理
                    OnMessageReceived(new MessageReceivedEventArgs(jsonRequest, typeof(object)));
                    McpLogger.Debug("已触发 MessageReceived 事件");

                    // 保持连接开放，等待响应通过SendAsync发送
                    // 注意：这里不关闭响应，让它保持开放以发送SSE事件
                }
                else if (supportsJSON)
                {
                    // 客户端只支持JSON，我们仍然需要处理请求但以JSON响应
                    McpLogger.Warning("客户端只支持JSON响应，但MCP规范要求SSE流");
                    OnMessageReceived(new MessageReceivedEventArgs(jsonRequest, typeof(object)));

                    response.StatusCode = 202;
                    response.ContentType = "application/json";
                    var acceptedResponse = "{\"status\":\"accepted\",\"note\":\"Response will be sent via SSE if available\"}";
                    var responseBytes = Encoding.UTF8.GetBytes(acceptedResponse);
                    response.ContentLength64 = responseBytes.Length;
                    await response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                    response.Close();
                }
                else
                {
                    McpLogger.Error("客户端Accept头不包含支持的内容类型");
                    response.StatusCode = 406; // Not Acceptable
                    response.Close();
                }
            }
        }

        private async Task HandleEventStream(HttpListenerResponse response, string connectionId, CancellationToken cancellationToken)
        {
            McpLogger.Debug("设置独立 SSE 连接 [{0}] (GET /events)", connectionId);
            response.ContentType = "text/event-stream";
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");
            response.StatusCode = 200;

            _activeConnections[connectionId] = response;
            McpLogger.Debug("SSE 连接已添加到活跃连接列表 [{0}]，当前连接数: {1}", connectionId, _activeConnections.Count);

            try
            {
                // 发送初始连接确认
                var welcomeEvent = "data: {\"type\":\"connected\"}\n\n";
                var welcomeBytes = Encoding.UTF8.GetBytes(welcomeEvent);
                await response.OutputStream.WriteAsync(welcomeBytes, 0, welcomeBytes.Length, cancellationToken);
                await response.OutputStream.FlushAsync(cancellationToken);
                McpLogger.Debug("已发送欢迎消息到 SSE 连接 [{0}]", connectionId);

                // 保持连接活跃，定期发送心跳
                while (!cancellationToken.IsCancellationRequested && response.OutputStream.CanWrite)
                {
                    await Task.Delay(30000, cancellationToken); // 30 秒心跳

                    var heartbeat = "data: {\"type\":\"heartbeat\"}\n\n";
                    var heartbeatBytes = Encoding.UTF8.GetBytes(heartbeat);
                    await response.OutputStream.WriteAsync(heartbeatBytes, 0, heartbeatBytes.Length, cancellationToken);
                    await response.OutputStream.FlushAsync(cancellationToken);
                    McpLogger.Debug("已发送心跳到 SSE 连接 [{0}]", connectionId);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                McpLogger.Error("SSE 连接处理时发生错误 [{0}]", ex);
                OnErrorOccurred(ex);
            }
            finally
            {
                try
                {
                    response.Close();
                    McpLogger.Debug("SSE 连接已关闭 [{0}]", connectionId);
                }
                catch
                {
                    // 忽略关闭时的错误
                }
            }
        }

        private void OnMessageReceived(MessageReceivedEventArgs args)
        {
            MessageReceived?.Invoke(this, args);
        }

        private void OnErrorOccurred(Exception exception)
        {
            ErrorOccurred?.Invoke(this, exception);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _disposeCts.Cancel();

            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();

                _serverTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // 忽略超时和释放错误
            }

            _disposeCts.Dispose();
        }
    }
}