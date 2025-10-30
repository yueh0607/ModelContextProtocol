using System;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server;
using ModelContextProtocol.Unity.Runtime.Transport;
using UnityEditor;
using UnityEngine;

namespace ModelContextProtocol.Unity.Editor
{
    /// <summary>
    /// 使用修复版 HTTP 传输启动/停止 MCP 服务器（Editor 菜单）
    /// </summary>
    public static class McpServerEditorHttpBootstrapFixed
    {
        private static TransportBasedMcpServer _server;
        private static CancellationTokenSource _cts;

        // 在域重载与编辑器退出时，确保服务器被正确停止，避免挂起阻塞
        static McpServerEditorHttpBootstrapFixed()
        {
            AssemblyReloadEvents.beforeAssemblyReload += StopServer;
            EditorApplication.quitting += StopServer;
        }

        [MenuItem("MCP Server/Start (HTTP 8767 Fixed)")]
        public static async void StartServer()
        {
            if (_server != null)
            {
                Debug.LogWarning("[MCP] Server already running");
                return;
            }

            var transport = new UnityHttpTransportFixed(8767);
            var loggingTransport = new LoggingTransport(transport, "HTTP:8767");

            var options = new McpServerOptions
            {
                ServerInfo = new Implementation { Name = "Unity MCP Server (HTTP Fixed)", Version = "1.0.0" },
                Capabilities = new ServerCapabilities()
            };

            // 注册 echo 工具
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

            // 注册 get_unity_info 工具
            options.ToolCollection.Add(
                SimpleMcpServerTool.Create(
                    name: "get_unity_info",
                    description: "获取 Unity Editor 信息",
                    handler: (args, ct) =>
                    {
                        var info = new
                        {
                            unityVersion = Application.unityVersion,
                            platform = Application.platform.ToString(),
                            productName = Application.productName,
                            companyName = Application.companyName,
                            isEditor = Application.isEditor
                        };

                        var result = new CallToolResult();
                        result.Content.Add(new TextContentBlock { Text = Newtonsoft.Json.JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented) });
                        return Task.FromResult(result);
                    }));

            _server = new TransportBasedMcpServer(loggingTransport, options);
            _cts = new CancellationTokenSource();

            Debug.Log("[MCP] Starting HTTP server on port 8767 (Fixed version)...");

            try
            {
                await _server.RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[MCP] Server stopped by cancellation");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP] Server error: {ex.Message}");
                StopServer();
            }
        }

        [MenuItem("MCP Server/Stop (HTTP Fixed)")]
        public static void StopServer()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _server = null;
            Debug.Log("[MCP] Server stopped");
        }
    }
}

