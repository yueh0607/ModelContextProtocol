using System;
using System.Threading;
using System.Threading.Tasks;
using JsonRpc.Models;

namespace JsonRpc.Transport
{
    public interface IJsonRpcTransport : IDisposable
    {
        Task SendAsync(object message, CancellationToken cancellationToken = default);
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
        event EventHandler<Exception> ErrorOccurred;
        bool IsConnected { get; }
        Task StartReceivingAsync(CancellationToken cancellationToken = default);
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public string MessageJson { get; set; }
        public Type ExpectedType { get; set; }

        public MessageReceivedEventArgs(string messageJson, Type expectedType)
        {
            MessageJson = messageJson;
            ExpectedType = expectedType;
        }

        public T Deserialize<T>() where T : class
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(MessageJson);
        }
    }
}