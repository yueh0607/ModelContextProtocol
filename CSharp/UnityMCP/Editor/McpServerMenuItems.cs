using UnityEditor;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity编辑器菜单项和快捷操作
    /// </summary>
    public static class McpServerMenuItems
    {
        [MenuItem("Tools/MCP Server/Open Server Window")]
        public static void OpenServerWindow()
        {
            McpServerEditorWindow.ShowWindow();
        }

        [MenuItem("Tools/MCP Server/Create Runtime Server in Scene")]
        public static void CreateRuntimeServerInScene()
        {
            // 检查是否已存在
            var existing = Object.FindObjectOfType<UnityMcpServer>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("MCP Server",
                    "A UnityMcpServer already exists in the scene.",
                    "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }

            // 创建GameObject
            GameObject serverObj = new GameObject("MCP Server (Runtime)");
            serverObj.AddComponent<UnityMcpServer>();
            // serverObj.AddComponent<Examples.UnityMcpServerExample>();

            // 选中并定位
            Selection.activeGameObject = serverObj;
            EditorGUIUtility.PingObject(serverObj);

            Debug.Log("Created MCP Server GameObject. This server runs only in Play mode.");
            Debug.Log("For Editor mode server, use Window → MCP Server");
        }

        [MenuItem("Tools/MCP Server/Documentation/Quick Start")]
        public static void OpenQuickStart()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Scripts/UnityMCP/QUICKSTART.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found",
                    "QUICKSTART.md not found. Please ensure the UnityMCP folder is properly installed.",
                    "OK");
            }
        }

        [MenuItem("Tools/MCP Server/Documentation/Full README")]
        public static void OpenReadme()
        {
            string path = System.IO.Path.Combine(Application.dataPath, "Scripts/UnityMCP/README.md");
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + path);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation Not Found",
                    "README.md not found. Please ensure the UnityMCP folder is properly installed.",
                    "OK");
            }
        }

        [MenuItem("Tools/MCP Server/Check Dependencies")]
        public static void CheckDependencies()
        {
            bool hasNewtonsoft = false;

            // 检查Newtonsoft.Json
            try
            {
                var assembly = System.Reflection.Assembly.Load("Newtonsoft.Json");
                hasNewtonsoft = assembly != null;
            }
            catch
            {
                hasNewtonsoft = false;
            }

            if (hasNewtonsoft)
            {
                EditorUtility.DisplayDialog("Dependencies Check",
                    "✅ All dependencies are installed!\n\n" +
                    "• Newtonsoft.Json: Installed",
                    "OK");
            }
            else
            {
                bool install = EditorUtility.DisplayDialog("Missing Dependencies",
                    "❌ Newtonsoft.Json is not installed.\n\n" +
                    "UnityMCP requires Newtonsoft.Json to function.\n\n" +
                    "Would you like to install it now?",
                    "Install via Package Manager", "Cancel");

                if (install)
                {
                    UnityEditor.PackageManager.Client.Add("com.unity.nuget.newtonsoft-json");
                    Debug.Log("Installing Newtonsoft.Json package...");
                }
            }
        }

        [MenuItem("Tools/MCP Server/About")]
        public static void ShowAbout()
        {
            EditorUtility.DisplayDialog("Unity MCP Server",
                "Unity MCP Server v1.0.0\n\n" +
                "A Model Context Protocol (MCP) server implementation for Unity.\n\n" +
                "Enables AI assistants like Claude to interact with your Unity projects.\n\n" +
                "Features:\n" +
                "• Editor mode server (no Play required)\n" +
                "• Runtime mode server (in-game)\n" +
                "• Full MCP protocol support\n" +
                "• Extensible tools and resources\n\n" +
                "For more information, see Tools → MCP Server → Documentation",
                "OK");
        }

        // 快捷键：Ctrl+Shift+M (Windows/Linux) 或 Cmd+Shift+M (macOS)
        [MenuItem("Tools/MCP Server/Open Server Window %#m")]
        public static void OpenServerWindowShortcut()
        {
            OpenServerWindow();
        }
    }
}
