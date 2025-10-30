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
    /// 使用 HTTP 传输启动/停止 MCP 服务器（Editor 菜单）。
    /// </summary>
    public static class McpServerEditorHttpBootstrap
    {
        private static TransportBasedMcpServer _server;
        private static CancellationTokenSource _cts;

        
        public static async void StartServer()
        {
            if (_server != null)
                return;

            var transport = new UnityHttpTransport(8767);

            var options = new McpServerOptions
            {
                ServerInfo = new Implementation { Name = "Unity MCP Server (HTTP)", Version = "1.0.0" },
                Capabilities = new ServerCapabilities()
            };

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
                StopServer();
            }
        }
        
        public static void StopServer()
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;
            _server = null;
        }
    }
}


