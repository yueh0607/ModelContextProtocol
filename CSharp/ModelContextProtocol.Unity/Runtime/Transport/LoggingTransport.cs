using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Server.Transport;
using UnityEngine;

namespace ModelContextProtocol.Unity.Editor
{
    /// <summary>
    /// 传输日志代理：包装 IMcpTransport，打印收发的原始 JSON 文本到 Unity 控制台。
    /// </summary>
    public sealed class LoggingTransport : IMcpTransport
    {
        private readonly IMcpTransport _inner;
        private readonly string _name;

        public LoggingTransport(IMcpTransport inner, string name)
        {
            _inner = inner;
            _name = name;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Debug.Log($"[MCP][{_name}] Transport Start");
            return _inner.StartAsync(cancellationToken);
        }

        public void Stop()
        {
            Debug.Log($"[MCP][{_name}] Transport Stop");
            _inner.Stop();
        }

        public async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            var msg = await _inner.ReadMessageAsync(cancellationToken);
            if (msg != null)
                Debug.Log($"[MCP][{_name}] <= {msg}");
            return msg;
        }

        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            Debug.Log($"[MCP][{_name}] => {message}");
            await _inner.WriteMessageAsync(message, cancellationToken);
        }
    }
}


