using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace UnityAIStudio.McpServer.Services
{
    /// <summary>
    /// 持久化的 MCP Server 管理器，在编译和窗口关闭时保持服务器运行
    /// </summary> 
    [InitializeOnLoad]
    public static class McpServerManager
    {
        private static IMcpServerService instance;
        private static bool isInitialized = false;
        private static bool isHandlingCompilation = false;

        // Persistent log buffer across window open/close
        private static readonly System.Collections.Generic.List<string> s_logs = new System.Collections.Generic.List<string>();
        private static int s_maxLogLines = 1000; // global cap to persist logs

        static McpServerManager()
        {
            // Load global log capacity
            s_maxLogLines = UnityEditor.EditorPrefs.GetInt("UnityAIStudio.McpServer.Logs.MaxLines.Global", 1000);

            // 注册编译事件
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;

            // 检测编译完成后恢复状态
            bool shouldRestore = EditorPrefs.GetBool("McpServer_WasRunningBeforeCompile", false);
            bool autoRestart = EditorPrefs.GetBool("McpServer_AutoRestartAfterCompile", true); // 默认开启

            if (shouldRestore && autoRestart)
            {
                EditorPrefs.DeleteKey("McpServer_WasRunningBeforeCompile");
                int port = EditorPrefs.GetInt("McpServer_PortBeforeCompile", 8080);
                EditorPrefs.DeleteKey("McpServer_PortBeforeCompile");

                // 延迟启动，确保编译完全完成
                EditorApplication.delayCall += () =>
                {
                    System.Threading.Thread.Sleep(1000); // 等待端口完全释放
                    Debug.Log($"[MCP Server Manager] Restoring server state after compilation on port {port}...");
                    GetOrCreateInstance().Start(port);
                };
            }
            else if (shouldRestore && !autoRestart)
            {
                // 清除标记但不重启
                EditorPrefs.DeleteKey("McpServer_WasRunningBeforeCompile");
                EditorPrefs.DeleteKey("McpServer_PortBeforeCompile");
                Debug.Log("[MCP Server Manager] Auto-restart is disabled. Server was not restarted after compilation.");
            }
        }

        private static void OnCompilationStarted(object obj)
        {
            if (instance != null && !isHandlingCompilation)
            {
                var state = instance.State;
                if (state.Status == Models.ServerStatus.Running)
                {
                    isHandlingCompilation = true;
                    int port = state.CurrentPort;

                    Debug.Log($"[MCP Server Manager] Compilation started. Stopping server on port {port}...");

                    // 保存状态到 EditorPrefs
                    EditorPrefs.SetBool("McpServer_WasRunningBeforeCompile", true);
                    EditorPrefs.SetInt("McpServer_PortBeforeCompile", port);

                    // 停止服务器以释放端口
                    try
                    {
                        instance.Stop();
                        Debug.Log("[MCP Server Manager] Server stopped successfully before compilation.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[MCP Server Manager] Error stopping server: {ex.Message}");
                    }
                }
            }
        }

        private static void OnCompilationFinished(object obj)
        {
            isHandlingCompilation = false;
            Debug.Log("[MCP Server Manager] Compilation finished.");
        }

        /// <summary>
        /// 获取或创建服务器实例（单例）
        /// </summary>
        public static IMcpServerService GetOrCreateInstance()
        {
            if (instance == null)
            {
                instance = new McpServerServiceCore();
                isInitialized = true;
                Debug.Log("[MCP Server Manager] Server instance created");

                // Subscribe to logs to persist across window lifecycle
                instance.OnLogMessage += AppendLog;
            }
            return instance;
        }

        /// <summary>
        /// 获取现有实例（不创建新的）
        /// </summary>
        public static IMcpServerService GetInstance()
        {
            return instance;
        }

        /// <summary>
        /// 检查实例是否存在
        /// </summary>
        public static bool HasInstance()
        {
            return instance != null;
        }

        /// <summary>
        /// 销毁实例（仅在需要完全关闭时调用）
        /// </summary>
        public static void DestroyInstance()
        {
            if (instance != null)
            {
                instance.OnLogMessage -= AppendLog;
                instance.Dispose();
                instance = null;
                isInitialized = false;
                Debug.Log("[MCP Server Manager] Server instance destroyed");
            }
        }

        /// <summary>
        /// 更新服务器（需要从外部定期调用）
        /// </summary>
        public static void Update()
        {
            instance?.Update();
        }

        /// <summary>
        /// 获取是否启用编译后自动重启
        /// </summary>
        public static bool GetAutoRestartAfterCompile()
        {
            return EditorPrefs.GetBool("McpServer_AutoRestartAfterCompile", true);
        }

        /// <summary>
        /// 设置是否启用编译后自动重启
        /// </summary>
        public static void SetAutoRestartAfterCompile(bool enabled)
        {
            EditorPrefs.SetBool("McpServer_AutoRestartAfterCompile", enabled);
            Debug.Log($"[MCP Server Manager] Auto-restart after compile: {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// 获取当前配置端口（优先返回运行端口；否则返回配置端口；再否则读 EditorPrefs）。
        /// </summary>
        public static int GetConfiguredPort()
        {
            if (instance != null)
            {
                var running = instance.State?.CurrentPort ?? 0;
                if (running > 0) return running;
                var cfg = instance.Config?.port ?? 0;
                if (cfg > 0) return cfg;
            }
            return EditorPrefs.GetInt("McpServer_Port", 8080);
        }

        // ===================== Logs Persistence API =====================

        private static void AppendLog(string message)
        {
            if (message == null) return;
            s_logs.Add(message);
            TrimLogs();
        }

        private static void TrimLogs()
        {
            while (s_logs.Count > s_maxLogLines)
            {
                s_logs.RemoveAt(0);
            }
        }

        public static System.Collections.Generic.List<string> GetLogs()
        {
            return new System.Collections.Generic.List<string>(s_logs);
        }

        public static void ClearLogs()
        {
            s_logs.Clear();
        }

        public static void SetLogCapacity(int maxLines)
        {
            s_maxLogLines = Mathf.Clamp(maxLines, 10, 100000);
            EditorPrefs.SetInt("UnityAIStudio.McpServer.Logs.MaxLines.Global", s_maxLogLines);
            TrimLogs();
        }
    }
}
