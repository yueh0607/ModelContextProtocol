using System;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server;
using ModelContextProtocol.Unity.Runtime.Transport;
using UnityEditor;

namespace ModelContextProtocol.Unity.Editor
{
    /// <summary>
    /// Unity Editor 下启动/停止 MCP 服务器的简单入口（无窗口）。
    /// </summary>
    public static class McpServerEditorBootstrap
    {
        private static TransportBasedMcpServer _server;
        private static CancellationTokenSource _cts;

        [MenuItem("MCP Server/Start (TCP 8765)")]
        public static async void StartServer()
        {
            if (_server != null)
                return;

            var transport = new UnityTcpTransport(8765);

            var options = new McpServerOptions
            {
                ServerInfo = new Implementation { Name = "Unity MCP Server", Version = "1.0.0" },
                Capabilities = new ServerCapabilities()
            };

            // 可选：添加一个简单工具，便于联调
            options.ToolCollection.Add(
                SimpleMcpServerTool.Create(
                    name: "echo",
                    description: "回显传入的 arguments",
                    handler: (args, ct) =>
                    {
                        var result = new CallToolResult();
                        result.Content.Add(new TextContentBlock { Text = args?.ToString() ?? "{}" });
                        return Task.FromResult(result);
                    }));

            _server = new TransportBasedMcpServer(transport, options);
            _cts = new CancellationTokenSource();

            try
            {
                await _server.RunAsync(_cts.Token);
            }
            catch (Exception)
            {
                // 只做演示使用，实际可记录到 Unity 控制台
                StopServer();
            }
        }

        [MenuItem("MCP Server/Stop")]
        public static void StopServer()
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;
            _server = null;
        }
    }
}


