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

        static McpServerManager()
        {
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
    }
}
