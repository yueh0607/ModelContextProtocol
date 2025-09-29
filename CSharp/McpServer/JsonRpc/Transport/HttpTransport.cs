using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;
using Newtonsoft.Json;

namespace JsonRpc.Transport
{
    public class HttpTransport : IJsonRpcTransport
    {
        private readonly HttpClient _httpClient;
        private readonly string _serverUrl;
        private readonly JsonSerializerSettings _serializerSettings;
        private bool _disposed = false;

        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public bool IsConnected => !_disposed;

        public HttpTransport(string serverUrl, HttpClient httpClient = null, JsonSerializerSettings serializerSettings = null)
        {
            _serverUrl = serverUrl ?? throw new ArgumentNullException(nameof(serverUrl));
            _httpClient = httpClient ?? new HttpClient();
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
                throw new ObjectDisposedException(nameof(HttpTransport));

            try
            {
                var json = JsonConvert.SerializeObject(message, _serializerSettings);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_serverUrl, content, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseJson))
                    {
                        OnMessageReceived(new MessageReceivedEventArgs(responseJson, typeof(object)));
                    }
                }
                else
                {
                    OnErrorOccurred(new HttpRequestException($"HTTP request failed with status: {response.StatusCode}"));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                throw;
            }
        }

        public Task StartReceivingAsync(CancellationToken cancellationToken = default)
        {
            // For HTTP transport, receiving is handled in SendAsync
            // This method is kept for interface compatibility
            return Task.CompletedTask;
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
            _httpClient?.Dispose();
        }
    }
}