using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;
using MapleModelContextProtocol.Server;
using MapleModelContextProtocol.Server.Transport;
using ModelContextProtocol.Unity.Runtime.Transport;
using UnityEditor;
using UnityEngine;

namespace ModelContextProtocol.Unity.Editor
{
    public sealed class McpServerWindow : EditorWindow
    {
        private enum TransportKind
        {
            WebSocket,
            Http,
            Tcp
        }

        // UI State
        private TransportKind _transportKind = TransportKind.WebSocket;
        private int _port = 8766;
        private Vector2 _logScroll;
        private readonly List<string> _logs = new List<string>();
        private readonly object _logLock = new object();
        private bool _autoScroll = true;

        // Server State
        private TransportBasedMcpServer _server;
        private CancellationTokenSource _cts;

        [MenuItem("MCP Server/Window")]
        public static void Open()
        {
            var win = GetWindow<McpServerWindow>(true, "MCP Server", true);
            win.minSize = new Vector2(520, 360);
            win.Show();
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleUnityLog;
            StopServerInternal();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            _transportKind = (TransportKind)EditorGUILayout.EnumPopup("Transport", _transportKind);
            _port = EditorGUILayout.IntField("Port", _port);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _server == null;
                if (GUILayout.Button("Connect", GUILayout.Height(24)))
                {
                    StartServerInternal();
                }
                GUI.enabled = _server != null;
                if (GUILayout.Button("Disconnect", GUILayout.Height(24)))
                {
                    StopServerInternal();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Clear Logs", GUILayout.Width(100)))
                {
                    lock (_logLock)
                    {
                        _logs.Clear();
                    }
                }
                _autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll");
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Logs", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll);
            string[] snapshot;
            lock (_logLock)
            {
                snapshot = _logs.ToArray();
            }
            foreach (var line in snapshot)
            {
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
            }
            if (_autoScroll)
            {
                _logScroll.y = float.MaxValue;
            }
            EditorGUILayout.EndScrollView();
        }

        private void StartServerInternal()
        {
            if (_server != null)
                return;

            try
            {
                IMcpTransport baseTransport;
                string name;
                switch (_transportKind)
                {
                    case TransportKind.WebSocket:
                        baseTransport = new UnityWebSocketTransport(_port);
                        name = $"WS:{_port}";
                        break;
                    case TransportKind.Http:
                        baseTransport = new UnityHttpTransport(_port);
                        name = $"HTTP:{_port}";
                        break;
                    default:
                        baseTransport = new UnityTcpTransport(_port);
                        name = $"TCP:{_port}";
                        break;
                }

                // 包装日志代理
                var transport = new LoggingTransport(baseTransport, name);

                var options = new McpServerOptions
                {
                    ServerInfo = new Implementation { Name = "Unity MCP Server (Editor)", Version = "1.0.0" },
                    Capabilities = new ServerCapabilities()
                };

                // 内置 echo 工具
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

                // 在后台跑，不阻塞 Editor
                Task.Run(async () =>
                {
                    try
                    {
                        Append("Server starting...");
                        await _server.RunAsync(_cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Append($"Server error: {ex.Message}");
                    }
                    finally
                    {
                        Append("Server stopped.");
                    }
                });

                Append($"Connected via {name}");
            }
            catch (Exception ex)
            {
                Append($"Start failed: {ex.Message}");
                StopServerInternal();
            }
        }

        private void StopServerInternal()
        {
            if (_server == null)
                return;
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;
            _server = null;
            Append("Disconnected");
        }

        private void HandleUnityLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log)
                Append(condition);
        }

        private void Append(string line)
        {
            lock (_logLock)
            {
                _logs.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
            }
            // 将重绘调度到主线程
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                    Repaint();
            };
        }
    }
}


