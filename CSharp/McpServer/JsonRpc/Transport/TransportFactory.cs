using System;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;

namespace JsonRpc.Transport
{
    public enum TransportType
    {
        Stdio,
        StreamableHttp
    }

    public static class TransportFactory
    {
        // ========== 普通用户接口 - 开箱即用 ==========

        /// <summary>
        /// 创建标准输入输出传输 - 用于命令行工具和进程间通信
        /// </summary>
        /// <returns>配置好的 stdio 传输</returns>
        public static IJsonRpcTransport CreateStdioTransport()
        {
            return new StreamTransport(
                Console.OpenStandardInput(),
                Console.OpenStandardOutput(),
                CreateDefaultJsonSettings()
            );
        }

        /// <summary>
        /// 创建流式 HTTP 传输 - 用于网络通信
        /// </summary>
        /// <param name="serverUrl">MCP 服务器地址 (如: "http://localhost:8080/mcp")</param>
        /// <returns>配置好的 StreamableHttp 传输</returns>
        public static IJsonRpcTransport CreateStreamableHttpTransport(string serverUrl)
        {
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentException("服务器地址不能为空", nameof(serverUrl));

            return new StreamableHttpTransport(
                serverUrl,
                null, // 使用默认 HttpClient
                CreateDefaultJsonSettings()
            );
        }

        // ========== 高级用户接口 - 自定义配置 ==========

        /// <summary>
        /// 创建自定义配置的 stdio 传输 (高级用户)
        /// </summary>
        /// <param name="inputStream">输入流</param>
        /// <param name="outputStream">输出流</param>
        /// <param name="serializerSettings">JSON 序列化设置</param>
        /// <returns>配置好的 stdio 传输</returns>
        public static IJsonRpcTransport CreateStdioTransport(Stream inputStream, Stream outputStream, JsonSerializerSettings serializerSettings = null)
        {
            return new StreamTransport(
                inputStream ?? throw new ArgumentNullException(nameof(inputStream)),
                outputStream ?? throw new ArgumentNullException(nameof(outputStream)),
                serializerSettings ?? CreateDefaultJsonSettings()
            );
        }

        /// <summary>
        /// 创建自定义配置的流式 HTTP 传输 (高级用户)
        /// </summary>
        /// <param name="serverUrl">服务器地址</param>
        /// <param name="httpClient">自定义 HTTP 客户端</param>
        /// <param name="serializerSettings">JSON 序列化设置</param>
        /// <returns>配置好的 StreamableHttp 传输</returns>
        public static IJsonRpcTransport CreateStreamableHttpTransport(string serverUrl, HttpClient httpClient, JsonSerializerSettings serializerSettings = null)
        {
            if (string.IsNullOrEmpty(serverUrl))
                throw new ArgumentException("服务器地址不能为空", nameof(serverUrl));

            return new StreamableHttpTransport(
                serverUrl,
                httpClient,
                serializerSettings ?? CreateDefaultJsonSettings()
            );
        }

        /// <summary>
        /// 创建基于配置选项的传输 (高级用户)
        /// </summary>
        /// <param name="type">传输类型</param>
        /// <param name="options">配置选项</param>
        /// <returns>配置好的传输</returns>
        public static IJsonRpcTransport CreateTransport(TransportType type, TransportOptions options = null)
        {
            switch (type)
            {
                case TransportType.Stdio:
                    var stdioOptions = options as StdioTransportOptions ?? new StdioTransportOptions();
                    return new StreamTransport(
                        stdioOptions.InputStream ?? Console.OpenStandardInput(),
                        stdioOptions.OutputStream ?? Console.OpenStandardOutput(),
                        stdioOptions.SerializerSettings ?? CreateDefaultJsonSettings()
                    );

                case TransportType.StreamableHttp:
                    var httpOptions = options as StreamableHttpTransportOptions ?? new StreamableHttpTransportOptions();

                    if (string.IsNullOrEmpty(httpOptions.ServerUrl))
                        throw new ArgumentException("StreamableHttp 传输需要指定服务器地址");

                    return new StreamableHttpTransport(
                        httpOptions.ServerUrl,
                        httpOptions.HttpClient,
                        httpOptions.SerializerSettings ?? CreateDefaultJsonSettings()
                    );

                default:
                    throw new ArgumentException($"不支持的传输类型: {type}");
            }
        }

        private static JsonSerializerSettings CreateDefaultJsonSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Formatting = Formatting.None
            };
        }
    }

    /// <summary>
    /// Base class for transport configuration options
    /// </summary>
    public abstract class TransportOptions
    {
        /// <summary>
        /// JSON serialization settings for message formatting
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; set; }
    }

    /// <summary>
    /// Configuration options for stdio transport (standard input/output streams)
    /// Used for process-to-process communication via stdin/stdout
    /// </summary>
    public class StdioTransportOptions : TransportOptions
    {
        /// <summary>
        /// Input stream for receiving JSON-RPC messages. Defaults to Console.OpenStandardInput()
        /// </summary>
        public Stream InputStream { get; set; }

        /// <summary>
        /// Output stream for sending JSON-RPC messages. Defaults to Console.OpenStandardOutput()
        /// </summary>
        public Stream OutputStream { get; set; }

        public StdioTransportOptions()
        {
            // Default values for stdio transport
            InputStream = null; // Will use Console.OpenStandardInput() if null
            OutputStream = null; // Will use Console.OpenStandardOutput() if null
        }
    }

    /// <summary>
    /// Configuration options for StreamableHttp transport
    /// Uses HTTP with Server-Sent Events (SSE) for bidirectional communication
    /// </summary>
    public class StreamableHttpTransportOptions : TransportOptions
    {
        /// <summary>
        /// Base URL of the MCP server (e.g., "http://localhost:8080/mcp")
        /// Required for StreamableHttp transport
        /// </summary>
        public string ServerUrl { get; set; }

        /// <summary>
        /// HTTP client instance for making requests. If null, a new HttpClient will be created
        /// </summary>
        public HttpClient HttpClient { get; set; }

        /// <summary>
        /// Timeout for HTTP requests in milliseconds. Defaults to 30000 (30 seconds)
        /// </summary>
        public int RequestTimeoutMs { get; set; }

        /// <summary>
        /// Whether to automatically reconnect on connection loss. Defaults to true
        /// </summary>
        public bool AutoReconnect { get; set; }

        /// <summary>
        /// Interval between reconnection attempts in milliseconds. Defaults to 5000 (5 seconds)
        /// </summary>
        public int ReconnectIntervalMs { get; set; }

        public StreamableHttpTransportOptions()
        {
            // Default values for StreamableHttp transport
            ServerUrl = "http://localhost:3000/mcp/"; // Must be set by user
            HttpClient = null; // Will create new HttpClient if null
            RequestTimeoutMs = 30000; // 30 seconds
            AutoReconnect = true;
            ReconnectIntervalMs = 5000; // 5 seconds
        }
    }
}