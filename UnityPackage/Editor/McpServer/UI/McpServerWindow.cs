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
            // Initialize service - 使用基于Core的新实现
            serverService = new McpServerServiceCore();

            // Initialize view
            serverView = new McpServerView(serverService);
            serverView.OnEnable();

            // Initialize styles
            McpUIStyles.Initialize();
        }

        private void OnDisable()
        {
            serverView?.OnDisable();
            serverService?.Dispose();
        }

        private void OnGUI()
        {
            McpUIStyles.Initialize();

            // Draw server view
            serverView?.OnGUI();
        }

        private void Update()
        {
            serverService?.Update();
            Repaint();
        }
    }
}
