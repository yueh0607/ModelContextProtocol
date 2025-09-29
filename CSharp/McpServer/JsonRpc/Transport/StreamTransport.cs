using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;
using Newtonsoft.Json;

namespace JsonRpc.Transport
{
    public class StreamTransport : IJsonRpcTransport
    {
        private readonly Stream _inputStream;
        private readonly Stream _outputStream;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _disposeCts = new CancellationTokenSource();
        private bool _disposed = false;

        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public bool IsConnected => !_disposed && _inputStream.CanRead && _outputStream.CanWrite;

        public StreamTransport(Stream inputStream, Stream outputStream, JsonSerializerSettings serializerSettings = null)
        {
            _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));

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
                throw new ObjectDisposedException(nameof(StreamTransport));

            try
            {
                var json = JsonConvert.SerializeObject(message, _serializerSettings);
                var bytes = Encoding.UTF8.GetBytes(json + "\n");

                await _writeLock.WaitAsync(cancellationToken);
                try
                {
                    await _outputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    await _outputStream.FlushAsync(cancellationToken);
                }
                finally
                {
                    _writeLock.Release();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex);
                throw;
            }
        }

        public async Task StartReceivingAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StreamTransport));

            var reader = new StreamReader(_inputStream, Encoding.UTF8, true, 1024);
            var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);

            try
            {
                while (!combinedCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var line = await reader.ReadLineAsync();

                        if (line == null)
                        {
                            // End of stream reached
                            break;
                        }

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // Fire event with raw JSON - let consumers decide how to deserialize
                        OnMessageReceived(new MessageReceivedEventArgs(line, typeof(object)));
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        OnErrorOccurred(ex);
                        break;
                    }
                }
            }
            finally
            {
                combinedCts?.Dispose();
                // Don't dispose reader as it would close the underlying stream
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
                _writeLock.Dispose();
                _disposeCts.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}