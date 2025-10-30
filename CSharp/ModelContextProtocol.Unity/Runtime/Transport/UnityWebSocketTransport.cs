using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Server.Transport;

namespace ModelContextProtocol.Unity.Runtime.Transport
{
    /// <summary>
    /// 基于 WebSocket 的 Unity 传输层实现（Editor 环境运行）。
    /// 仅支持单连接、文本帧、无分片、UTF-8。
    /// </summary>
    public sealed class UnityWebSocketTransport : IMcpTransport, IDisposable
    {
        private readonly int _port;
        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private volatile bool _stopped;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public UnityWebSocketTransport(int port = 8766)
        {
            _port = port;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _stopped = false;
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
            _stream = _client.GetStream();

            // WebSocket 握手
            await PerformHandshakeAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> ReadMessageAsync(CancellationToken cancellationToken)
        {
            if (_stopped || _stream == null)
                return null;

            try
            {
                // 读取帧头
                int b1 = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (b1 == -1) return null; // 断开
                int b2 = await ReadByteAsync(cancellationToken).ConfigureAwait(false);
                if (b2 == -1) return null;

                bool fin = (b1 & 0x80) != 0; // 仅支持 FIN=1
                int opcode = b1 & 0x0F;      // 仅支持文本 0x1
                bool masked = (b2 & 0x80) != 0; // 客户端->服务端应为 true
                ulong payloadLen = (ulong)(b2 & 0x7F);

                if (!fin) return null; // 不支持分片
                if (opcode == 0x8) return null; // Close 帧
                if (opcode != 0x1) return null; // 仅支持文本帧

                if (payloadLen == 126)
                {
                    // 16-bit
                    byte[] ext = await ReadExactlyAsync(2, cancellationToken).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) Array.Reverse(ext);
                    payloadLen = BitConverter.ToUInt16(ext, 0);
                }
                else if (payloadLen == 127)
                {
                    // 64-bit
                    byte[] ext = await ReadExactlyAsync(8, cancellationToken).ConfigureAwait(false);
                    if (BitConverter.IsLittleEndian) Array.Reverse(ext);
                    payloadLen = BitConverter.ToUInt64(ext, 0);
                }

                byte[] mask = null;
                if (masked)
                {
                    mask = await ReadExactlyAsync(4, cancellationToken).ConfigureAwait(false);
                }

                if (payloadLen > int.MaxValue) return null; // 简化处理
                byte[] payload = await ReadExactlyAsync((int)payloadLen, cancellationToken).ConfigureAwait(false);

                if (masked && mask != null)
                {
                    for (int i = 0; i < payload.Length; i++)
                    {
                        payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                    }
                }

                string text = Encoding.UTF8.GetString(payload);
                return text;
            }
            catch
            {
                return null;
            }
        }

        public async Task WriteMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (_stopped || _stream == null || string.IsNullOrEmpty(message))
                return;

            byte[] payload = Encoding.UTF8.GetBytes(message);

            // 服务器发给客户端不需要掩码
            byte op = 0x81; // FIN=1, text
            byte[] header;

            if (payload.Length <= 125)
            {
                header = new byte[] { op, (byte)payload.Length };
            }
            else if (payload.Length <= ushort.MaxValue)
            {
                header = new byte[4];
                header[0] = op;
                header[1] = 126;
                ushort len = (ushort)payload.Length;
                byte[] lenBytes = BitConverter.GetBytes(len);
                if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                header[2] = lenBytes[0];
                header[3] = lenBytes[1];
            }
            else
            {
                header = new byte[10];
                header[0] = op;
                header[1] = 127;
                ulong len = (ulong)payload.Length;
                byte[] lenBytes = BitConverter.GetBytes(len);
                if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
                for (int i = 0; i < 8; i++) header[2 + i] = lenBytes[i];
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await _stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
                await _stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Stop()
        {
            _stopped = true;
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); } catch { }
            try { _listener?.Stop(); } catch { }
        }

        public void Dispose()
        {
            Stop();
            _writeLock.Dispose();
        }

        private async Task PerformHandshakeAsync(CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(_stream, Encoding.ASCII, false, 1024, leaveOpen: true))
            using (var writer = new StreamWriter(_stream, Encoding.ASCII, 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true })
            {
                // 读取 HTTP 请求头
                string line;
                string webSocketKey = null;

                // 第一行：GET / HTTP/1.1
                line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(line) || !line.StartsWith("GET "))
                    throw new InvalidOperationException("Invalid WebSocket handshake");

                // 读取后续头部
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    var idx = line.IndexOf(':');
                    if (idx > 0)
                    {
                        var name = line.Substring(0, idx).Trim();
                        var value = line.Substring(idx + 1).Trim();
                        if (name.Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                        {
                            webSocketKey = value;
                        }
                    }
                }

                if (string.IsNullOrEmpty(webSocketKey))
                    throw new InvalidOperationException("Missing Sec-WebSocket-Key");

                string acceptKey = ComputeWebSocketAcceptKey(webSocketKey);

                // 返回握手响应
                await writer.WriteLineAsync("HTTP/1.1 101 Switching Protocols").ConfigureAwait(false);
                await writer.WriteLineAsync("Upgrade: websocket").ConfigureAwait(false);
                await writer.WriteLineAsync("Connection: Upgrade").ConfigureAwait(false);
                await writer.WriteLineAsync("Sec-WebSocket-Accept: " + acceptKey).ConfigureAwait(false);
                await writer.WriteLineAsync().ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
            }
        }

        private static string ComputeWebSocketAcceptKey(string clientKey)
        {
            // Magic GUID per RFC6455
            const string guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string concat = clientKey + guid;
            byte[] hash = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(concat));
            return Convert.ToBase64String(hash);
        }

        private async Task<int> ReadByteAsync(CancellationToken cancellationToken)
        {
            byte[] b = new byte[1];
            int r = await _stream.ReadAsync(b, 0, 1, cancellationToken).ConfigureAwait(false);
            return r == 1 ? b[0] : -1;
        }

        private async Task<byte[]> ReadExactlyAsync(int count, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await _stream.ReadAsync(buffer, offset, count - offset, cancellationToken).ConfigureAwait(false);
                if (read <= 0) throw new EndOfStreamException();
                offset += read;
            }
            return buffer;
        }
    }
}


