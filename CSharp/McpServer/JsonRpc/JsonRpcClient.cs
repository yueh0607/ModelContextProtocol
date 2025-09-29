using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;
using JsonRpc.Transport;

namespace JsonRpc.Client
{
    public class JsonRpcClient : IDisposable
    {
        private readonly IJsonRpcTransport _transport;
        private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonRpcResponse>> _pendingRequests = new ConcurrentDictionary<object, TaskCompletionSource<JsonRpcResponse>>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly Task _receiveTask;
        private long _requestId = 0;
        private bool _disposed = false;

        public event EventHandler<JsonRpcRequest> NotificationReceived;
        public event EventHandler<Exception> ErrorOccurred;

        public JsonRpcClient(IJsonRpcTransport transport)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _transport.ErrorOccurred += OnTransportError;
            _transport.MessageReceived += OnMessageReceived;

            _receiveTask = Task.Run(async () => await _transport.StartReceivingAsync(_cancellationTokenSource.Token));
        }

        public async Task<JsonRpcResponse> SendRequestAsync(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));

            var requestId = Interlocked.Increment(ref _requestId);
            var request = new JsonRpcRequest
            {
                Id = requestId,
                Method = method,
                Params = parameters
            };

            var tcs = new TaskCompletionSource<JsonRpcResponse>();
            _pendingRequests[requestId] = tcs;

            try
            {
                await _transport.SendAsync(request, cancellationToken);

                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                using (var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    combinedCts.Token.Register(() =>
                    {
                        if (_pendingRequests.TryRemove(requestId, out var pendingTcs))
                        {
                            if (timeoutCts.Token.IsCancellationRequested)
                            {
                                pendingTcs.TrySetException(new TimeoutException($"Request {requestId} timed out"));
                            }
                            else
                            {
                                pendingTcs.TrySetCanceled(cancellationToken);
                            }
                        }
                    });

                    return await tcs.Task;
                }
            }
            catch
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw;
            }
        }

        public async Task<T> SendRequestAsync<T>(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync(method, parameters, cancellationToken);

            if (response.IsError)
            {
                throw new JsonRpcException(response.Error);
            }

            if (response.Result == null)
            {
                return default(T);
            }

            if (response.Result is T result)
            {
                return result;
            }

            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(response.Result.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize result to {typeof(T).Name}", ex);
            }
        }

        public async Task SendNotificationAsync(string method, object parameters = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(JsonRpcClient));

            var request = new JsonRpcRequest
            {
                Id = null, // Notification has no ID
                Method = method,
                Params = parameters
            };

            await _transport.SendAsync(request, cancellationToken);
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                var messageJson = e.MessageJson;
                if (string.IsNullOrEmpty(messageJson))
                    return;

                // Try to parse as response first
                if (messageJson.Contains("\"result\"") || messageJson.Contains("\"error\""))
                {
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonRpcResponse>(messageJson);
                    if (response?.Id != null && _pendingRequests.TryRemove(response.Id, out var tcs))
                    {
                        tcs.TrySetResult(response);
                    }
                }
                // Try to parse as request (notification)
                else if (messageJson.Contains("\"method\""))
                {
                    var request = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonRpcRequest>(messageJson);
                    if (request != null && request.IsNotification)
                    {
                        NotificationReceived?.Invoke(this, request);
                    }
                }
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

            // Cancel all pending requests
            foreach (var kvp in _pendingRequests)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingRequests.Clear();

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

    public class JsonRpcException : Exception
    {
        public JsonRpcError Error { get; }

        public JsonRpcException(JsonRpcError error) : base(error.Message)
        {
            Error = error;
        }

        public JsonRpcException(int code, string message, object data = null) : base(message)
        {
            Error = new JsonRpcError
            {
                Code = code,
                Message = message,
                Data = data
            };
        }
    }
}