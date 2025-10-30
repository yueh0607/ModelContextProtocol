using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMCP
{
    /// <summary>
    /// Unity MCP服务器MonoBehaviour组件
    /// 在场景中添加此组件即可启动MCP服务器
    /// </summary>
    public class UnityMcpServer : MonoBehaviour
    {
        [Header("Server Configuration")]
        [Tooltip("服务器监听端口")]
        [SerializeField] private int port = 3000;

        [Tooltip("服务器名称")]
        [SerializeField] private string serverName = "Unity MCP Server";

        [Tooltip("服务器版本")]
        [SerializeField] private string serverVersion = "1.0.0";

        [Tooltip("是否在启动时自动开始")]
        [SerializeField] private bool autoStart = true;

        [Header("Debug")]
        [Tooltip("是否在控制台输出日志")]
        [SerializeField] private bool enableLogging = true;

        [Tooltip("最大日志条数")]
        [SerializeField] private int maxLogCount = 100;

        private McpServer _mcpServer;
        private McpHttpServer _httpServer;
        private List<string> _logs = new List<string>();
        private readonly object _logLock = new object();

        /// <summary>
        /// MCP核心服务器实例
        /// </summary>
        public McpServer McpServer => _mcpServer;

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning => _httpServer != null && _httpServer.IsRunning;

        /// <summary>
        /// 获取最近的日志
        /// </summary>
        public string[] GetRecentLogs()
        {
            lock (_logLock)
            {
                return _logs.ToArray();
            }
        }

        private void Awake()
        {
            // 创建MCP服务器
            _mcpServer = new McpServer
            {
                ServerName = serverName,
                ServerVersion = serverVersion
            };
            _mcpServer.OnLog += HandleLog;

            // 创建HTTP服务器
            _httpServer = new McpHttpServer(_mcpServer, port);
            _httpServer.OnLog += HandleLog;

            Log("Unity MCP Server initialized");
        }

        private void Start()
        {
            if (autoStart)
            {
                StartServer();
            }
        }

        private void Update()
        {
            // 处理HTTP服务器的主线程操作
            if (_httpServer != null)
            {
                _httpServer.Update();
            }
        }

        private void OnDestroy()
        {
            StopServer();
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }

        /// <summary>
        /// 启动MCP服务器
        /// </summary>
        public void StartServer()
        {
            if (IsRunning)
            {
                Log("Server is already running");
                return;
            }

            try
            {
                _httpServer.Start();
                Log($"MCP Server started successfully on port {port}");
                Log($"Connect your MCP client to: http://localhost:{port}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start server: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止MCP服务器
        /// </summary>
        public void StopServer()
        {
            if (!IsRunning) return;

            try
            {
                _httpServer.Stop();
                Log("MCP Server stopped");
            }
            catch (Exception ex)
            {
                LogError($"Error stopping server: {ex.Message}");
            }
        }

        /// <summary>
        /// 注册工具
        /// </summary>
        public void RegisterTool(McpTool tool)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            _mcpServer.RegisterTool(tool);
        }

        /// <summary>
        /// 注册资源
        /// </summary>
        public void RegisterResource(McpResource resource)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            _mcpServer.RegisterResource(resource);
        }

        /// <summary>
        /// 注册Prompt
        /// </summary>
        public void RegisterPrompt(McpPrompt prompt)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            _mcpServer.RegisterPrompt(prompt);
        }

        private void HandleLog(string message)
        {
            lock (_logLock)
            {
                _logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");

                // 限制日志数量
                while (_logs.Count > maxLogCount)
                {
                    _logs.RemoveAt(0);
                }
            }

            if (enableLogging)
            {
                Debug.Log(message);
            }
        }

        private void Log(string message)
        {
            HandleLog($"[UnityMcpServer] {message}");
        }

        private void LogError(string message)
        {
            HandleLog($"[UnityMcpServer] ERROR: {message}");
            if (enableLogging)
            {
                Debug.LogError(message);
            }
        }

        #region Editor UI
#if UNITY_EDITOR
        [ContextMenu("Start Server")]
        private void StartServerContextMenu()
        {
            StartServer();
        }

        [ContextMenu("Stop Server")]
        private void StopServerContextMenu()
        {
            StopServer();
        }

        [ContextMenu("Print Status")]
        private void PrintStatus()
        {
            Debug.Log($"Server Status: {(IsRunning ? "Running" : "Stopped")}");
            Debug.Log($"Port: {port}");
            Debug.Log($"Tools: {_mcpServer?.GetType().GetField("_tools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_mcpServer)?.GetType().GetProperty("Count")?.GetValue(_mcpServer?.GetType().GetField("_tools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_mcpServer)) ?? 0}");
        }
#endif
        #endregion
    }
}
