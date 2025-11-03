using UnityEngine;
using UnityEditor;
using UnityAIStudio.McpServer.Services;

namespace UnityAIStudio.McpServer.UI
{
    /// <summary>
    /// Main window for MCP Server Settings
    /// </summary>
    public class McpServerWindow : EditorWindow
    {
        private IMcpServerService serverService;
        private McpServerView serverView;

        [MenuItem("Window/MCP Server Settings")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpServerWindow>("MCP Server Settings");
            window.minSize = new Vector2(900, 700);
        }

        private void OnEnable()
        {
            // 使用持久化的服务管理器（而不是每次创建新实例）
            serverService = McpServerManager.GetOrCreateInstance();

            // Initialize view
            serverView = new McpServerView(serverService);
            serverView.OnEnable();
        }

        private void OnDisable()
        {
            // 只清理 view，不销毁 service（保持服务器运行）
            serverView?.OnDisable();
            // 注释掉：serverService?.Dispose();
            // 服务器将继续在后台运行，即使窗口关闭
        }

        private void OnGUI()
        {
            McpUIStyles.Initialize();

            // Draw server view
            serverView?.OnGUI();
        }

        private void Update()
        {
            // 通过管理器更新服务
            McpServerManager.Update();
            Repaint();
        }
    }
}
