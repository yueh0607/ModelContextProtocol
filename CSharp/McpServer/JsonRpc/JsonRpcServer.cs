using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;
using JsonRpc.Transport;
using McpServerLib.Utils;

namespace JsonRpc.Server
{
    public delegate Task<object> JsonRpcMethodHandler(object parameters, CancellationToken cancellationToken);

    public class JsonRpcServer : IDisposable
    {
        private readonly IJsonRpcTransport _transport;
        private readonly ConcurrentDictionary<string, JsonRpcMethodHandler> _methodHandlers = new ConcurrentDictionary<string, JsonRpcMethodHandler>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _receiveTask;
        private bool _disposed = false;

        public event EventHandler<JsonRpcRequest> RequestReceived;
        public event EventHandler<Exception> ErrorOccurred;

        public JsonRpcServer(IJsonRpcTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _transport.ErrorOccurred += OnTransportError;
            _transport.MessageReceived += OnMessageReceived;

            _receiveTask = Task.Run(async () => await _transport.StartReceivingAsync(_cancellationTokenSource.Token));
        }

        public void RegisterMethod(string method, JsonRpcMethodHandler handler)
        {
            if (string.IsNullOrWhiteSpace(method))
                throw new ArgumentException("Method name cannot be null or empty", nameof(method));

            _methodHandlers[method] = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public void RegisterMethod<TParams>(string method, Func<TParams, CancellationToken, Task<object>> handler)
        {
            RegisterMethod(method, async (parameters, cancellationToken) =>
            {
                TParams typedParams = default;

                if (parameters != null)
                {
                    if (parameters is TParams directParams)
                    {
                        typedParams = directParams;
                    }
                    else
                    {
                        try
                        {
                            typedParams = Newtonsoft.Json.JsonConvert.DeserializeObject<TParams>(parameters.ToString());
                        }
                        catch (Exception ex)
                        {
                            throw new JsonRpcException(JsonRpcErrorCodes.InvalidParams, $"Invalid parameters: {ex.Message}");
                        }
                    }
                }

                return await handler(typedParams, cancellationToken);
            });
        }

        public void RegisterMethod<TParams, TResult>(string method, Func<TParams, CancellationToken, Task<TResult>> handler)
        {
            RegisterMethod<TParams>(method, async (parameters, cancellationToken) =>
            {
                var result = await handler(parameters, cancellationToken);
                return result;
            });
        }

        public void UnregisterMethod(string method)
        {
            _methodHandlers.TryRemove(method, out _);
        }

        public async Task SendNotificationAsync(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcServer));

            var notification = new JsonRpcRequest
            {
                Id = null,
                Method = method,
                Params = parameters
            };

            await _transport.SendAsync(notification, cancellationToken);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                McpLogger.Debug("JsonRpcServer 收到消息: {0}", e.MessageJson);
                var request = e.Deserialize<JsonRpcRequest>();
                if (request == null)
                {
                    McpLogger.Warning("无法反序列化请求");
                    return;
                }

                McpLogger.Debug("处理 JSON-RPC 请求: Method={0}, Id={1}", request.Method, request.Id);

                // Fire event for request received
                RequestReceived?.Invoke(this, request);

                // Process request in background to avoid blocking the receive loop
                _ = Task.Run(async () => await ProcessRequestAsync(request, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                McpLogger.Error("处理接收到的消息时发生错误", ex);
                OnErrorOccurred(ex);
            }
        }

        private async Task ProcessRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
        {
            try
            {
                McpLogger.Debug("开始处理请求: Method={0}, Id={1}", request.Method, request.Id);

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Method))
                {
                    McpLogger.Warning("请求方法名为空");
                    if (!request.IsNotification)
                    {
                        await SendErrorResponseAsync(request.Id, JsonRpcErrorCodes.InvalidRequest, "Method name is required");
                    }
                    return;
                }

                // Check if method is registered
                if (!_methodHandlers.TryGetValue(request.Method, out var handler))
                {
                    McpLogger.Warning(string.Format("未找到方法处理器: {0}", request.Method));
                    if (!request.IsNotification)
                    {
                        await SendErrorResponseAsync(request.Id, JsonRpcErrorCodes.MethodNotFound, $"Method '{request.Method}' not found");
                    }
                    return;
                }

                McpLogger.Debug("找到方法处理器，开始执行: {0}", request.Method);

                // Execute method
                try
                {
                    var result = await handler(request.Params, cancellationToken);
                    McpLogger.Debug("方法执行完成: {0}, 结果类型: {1}", request.Method, result?.GetType()?.Name ?? "null");

                    // Send response only for requests (not notifications)
                    if (!request.IsNotification)
                    {
                        var response = new JsonRpcResponse
                        {
                            Id = request.Id,
                            Result = result
                        };

                        McpLogger.Debug("发送响应: Id={0}", request.Id);
                        await _transport.SendAsync(response, cancellationToken);
                        McpLogger.Debug("响应已发送: Id={0}", request.Id);
                    }
                }
                catch (JsonRpcException rpcEx)
                {
                    McpLogger.Error(string.Format("JSON-RPC 异常: {0}", rpcEx.Error.Message));
                    if (!request.IsNotification)
                    {
                        await SendErrorResponseAsync(request.Id, rpcEx.Error.Code, rpcEx.Error.Message, rpcEx.Error.Data);
                    }
                }
                catch (Exception ex)
                {
                    McpLogger.Error("执行方法时发生异常: {0}", ex);
                    if (!request.IsNotification)
                    {
                        await SendErrorResponseAsync(request.Id, JsonRpcErrorCodes.InternalError, "Internal error", ex.Message);
                    }
                    OnErrorOccurred(ex);
                }
            }
            catch (Exception ex)
            {
                McpLogger.Error("处理请求时发生异常", ex);
                OnErrorOccurred(ex);
            }
        }

        private async Task SendErrorResponseAsync(object requestId, int errorCode, string errorMessage, object errorData = null)
        {
            try
            {
                var errorResponse = new JsonRpcResponse
                {
                    Id = requestId,
                    Error = new JsonRpcError
                    {
                        Code = errorCode,
                        Message = errorMessage,
                        Data = errorData
                    }
                };

                await _transport.SendAsync(errorResponse);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
            }
        }

        private void OnTransportError(object sender, Exception exception)
        {
            OnErrorOccurred(exception);
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
            _cancellationTokenSource.Cancel();

            try
            {
                _receiveTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // Ignore timeout
            }

            _cancellationTokenSource.Dispose();
            _transport.Dispose();
        }
    }
}