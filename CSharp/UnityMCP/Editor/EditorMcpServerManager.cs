using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Editor
{
    /// <summary>
    /// 编辑器模式下的MCP服务器管理器
    /// 不依赖MonoBehaviour，使用EditorApplication.update
    /// </summary>
    public class EditorMcpServerManager
    {
        private McpServer _mcpServer;
        private McpHttpServer _httpServer;
        private bool _isInitialized = false;
        private List<string> _logs = new List<string>();
        private readonly object _logLock = new object();
        private const int MaxLogCount = 200;

        public int Port { get; set; } = 3000;
        public string ServerName { get; set; } = "Unity MCP Server (Editor)";
        public string ServerVersion { get; set; } = "1.0.0";
        public bool IsRunning => _httpServer != null && _httpServer.IsRunning;

        public event Action<string> OnLogMessage;

        public EditorMcpServerManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            // 创建MCP服务器
            _mcpServer = new McpServer
            {
                ServerName = ServerName,
                ServerVersion = ServerVersion
            };
            _mcpServer.OnLog += HandleLog;

            // 注册到EditorApplication.update
            EditorApplication.update += OnEditorUpdate;

            _isInitialized = true;
            Log("MCP Server Manager initialized in Editor mode");
        }

        ~EditorMcpServerManager()
        {
            Cleanup();
        }

        public void Cleanup()
        {
            StopServer();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            // 处理HTTP服务器的主线程操作
            if (_httpServer != null && _httpServer.IsRunning)
            {
                _httpServer.Update();
            }

            // 处理Unity主线程调度器
            Examples.UnityMainThreadDispatcher.Update();
        }

        public void StartServer()
        {
            if (IsRunning)
            {
                Log("Server is already running");
                return;
            }

            try
            {
                // 更新服务器信息
                _mcpServer.ServerName = ServerName;
                _mcpServer.ServerVersion = ServerVersion;

                // 创建HTTP服务器
                _httpServer = new McpHttpServer(_mcpServer, Port);
                _httpServer.OnLog += HandleLog;

                _httpServer.Start();
                Log($"✅ MCP Server started successfully on port {Port}");
                Log($"📡 Endpoint: http://localhost:{Port}");
                Log($"🔧 Tools: {GetToolCount()}, 📦 Resources: {GetResourceCount()}");
            }
            catch (Exception ex)
            {
                LogError($"Failed to start server: {ex.Message}");
            }
        }

        public void StopServer()
        {
            if (!IsRunning) return;

            try
            {
                _httpServer.Stop();
                _httpServer = null;
                Log("⏹ MCP Server stopped");
            }
            catch (Exception ex)
            {
                LogError($"Error stopping server: {ex.Message}");
            }
        }

        public void RegisterTool(McpTool tool)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            try
            {
                _mcpServer.RegisterTool(tool);
            }
            catch (Exception ex)
            {
                LogError($"Failed to register tool {tool?.Name}: {ex.Message}");
            }
        }

        public void RegisterResource(McpResource resource)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            try
            {
                _mcpServer.RegisterResource(resource);
            }
            catch (Exception ex)
            {
                LogError($"Failed to register resource {resource?.Uri}: {ex.Message}");
            }
        }

        public void RegisterPrompt(McpPrompt prompt)
        {
            if (_mcpServer == null)
            {
                LogError("MCP Server not initialized");
                return;
            }

            try
            {
                _mcpServer.RegisterPrompt(prompt);
            }
            catch (Exception ex)
            {
                LogError($"Failed to register prompt {prompt?.Name}: {ex.Message}");
            }
        }

        public int GetToolCount()
        {
            if (_mcpServer == null) return 0;

            var toolsField = typeof(McpServer).GetField("_tools",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var tools = toolsField?.GetValue(_mcpServer) as System.Collections.IDictionary;
            return tools?.Count ?? 0;
        }

        public int GetResourceCount()
        {
            if (_mcpServer == null) return 0;

            var resourcesField = typeof(McpServer).GetField("_resources",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resources = resourcesField?.GetValue(_mcpServer) as System.Collections.IDictionary;
            return resources?.Count ?? 0;
        }

        public List<McpTool> GetRegisteredTools()
        {
            if (_mcpServer == null) return new List<McpTool>();

            try
            {
                var toolsField = typeof(McpServer).GetField("_tools",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var tools = toolsField?.GetValue(_mcpServer) as Dictionary<string, McpTool>;
                return tools?.Values.ToList() ?? new List<McpTool>();
            }
            catch
            {
                return new List<McpTool>();
            }
        }

        public List<McpResource> GetRegisteredResources()
        {
            if (_mcpServer == null) return new List<McpResource>();

            try
            {
                var resourcesField = typeof(McpServer).GetField("_resources",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var resources = resourcesField?.GetValue(_mcpServer) as Dictionary<string, McpResource>;
                return resources?.Values.ToList() ?? new List<McpResource>();
            }
            catch
            {
                return new List<McpResource>();
            }
        }

        public string[] GetLogs()
        {
            lock (_logLock)
            {
                return _logs.ToArray();
            }
        }

        public void ClearLogs()
        {
            lock (_logLock)
            {
                _logs.Clear();
            }
            Log("Logs cleared");
        }

        private void HandleLog(string message)
        {
            lock (_logLock)
            {
                string timestampedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                _logs.Add(timestampedMessage);

                // 限制日志数量
                while (_logs.Count > MaxLogCount)
                {
                    _logs.RemoveAt(0);
                }
            }

            OnLogMessage?.Invoke(message);
        }

        private void Log(string message)
        {
            HandleLog($"[EditorManager] {message}");
        }

        private void LogError(string message)
        {
            HandleLog($"[EditorManager] ERROR: {message}");
            Debug.LogError(message);
        }
    }
}
