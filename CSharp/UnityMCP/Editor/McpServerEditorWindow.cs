using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityMCP.Editor
{
    /// <summary>
    /// Unity编辑器窗口 - MCP服务器控制面板
    /// 可在编辑器模式下运行MCP服务器，无需进入Play模式
    /// </summary>
    public class McpServerEditorWindow : EditorWindow
    {
        private static McpServerEditorWindow _instance;
        private EditorMcpServerManager _serverManager;

        // UI状态
        private Vector2 _logScrollPosition;
        private Vector2 _toolsScrollPosition;
        private bool _showLogs = true;
        private bool _showTools = true;
        private bool _showResources = true;
        private bool _autoScroll = true;

        // 配置
        private int _port = 3000;
        private string _serverName = "Unity MCP Server (Editor)";
        private string _serverVersion = "1.0.0";
        private bool _autoStart = false;
        private bool _verboseLogging = true;

        // EditorPrefs键
        private const string PREF_PORT = "McpServer_Port";
        private const string PREF_AUTO_START = "McpServer_AutoStart";
        private const string PREF_SERVER_NAME = "McpServer_Name";
        private const string PREF_VERBOSE = "McpServer_Verbose";

        [MenuItem("Window/MCP Server")]
        public static void ShowWindow()
        {
            _instance = GetWindow<McpServerEditorWindow>("MCP Server");
            _instance.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            _instance = this;
            LoadPreferences();
            InitializeServerManager();

            // 如果设置了自动启动，则启动服务器
            if (_autoStart && !_serverManager.IsRunning)
            {
                _serverManager.StartServer();
            }
        }

        private void OnDisable()
        {
            SavePreferences();
        }

        private void OnDestroy()
        {
            // 窗口关闭时停止服务器
            if (_serverManager != null && _serverManager.IsRunning)
            {
                _serverManager.StopServer();
            }
        }

        private void InitializeServerManager()
        {
            if (_serverManager == null)
            {
                _serverManager = new EditorMcpServerManager();
                _serverManager.Port = _port;
                _serverManager.ServerName = _serverName;
                _serverManager.ServerVersion = _serverVersion;
                _serverManager.OnLogMessage += HandleLogMessage;

                // 注册默认工具
                RegisterDefaultTools();
            }
        }

        private void RegisterDefaultTools()
        {
            try
            {
                _serverManager.RegisterTool(new Examples.UnityLogTool());
                _serverManager.RegisterTool(new Examples.UnitySceneInfoTool());
                _serverManager.RegisterTool(new Examples.UnityGameObjectTool());
                _serverManager.RegisterResource(new Examples.UnitySceneHierarchyResource());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to register default tools: {ex.Message}");
            }
        }

        private void HandleLogMessage(string message)
        {
            if (_verboseLogging)
            {
                Debug.Log(message);
            }

            // 触发窗口重绘 - 必须在主线程调用
            if (_instance != null)
            {
                // 使用EditorApplication.delayCall在主线程执行Repaint
                EditorApplication.delayCall += () =>
                {
                    if (_instance != null)
                    {
                        _instance.Repaint();
                    }
                };
            }
        }

        private void OnGUI()
        {
            if (_serverManager == null)
            {
                InitializeServerManager();
            }

            EditorGUILayout.BeginVertical();

            DrawHeader();
            DrawServerControls();
            DrawServerStatus();

            EditorGUILayout.Space(10);

            DrawToolsList();
            DrawResourcesList();
            DrawLogsSection();

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("🚀 Unity MCP Server", titleStyle);
            EditorGUILayout.LabelField("Model Context Protocol Server for Unity Editor",
                EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Space(5);
            DrawSeparator();
        }

        private void DrawServerControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Server Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(_serverManager.IsRunning);
            {
                int newPort = EditorGUILayout.IntField("Port", _port);
                if (newPort != _port)
                {
                    _port = newPort;
                    _serverManager.Port = _port;
                }

                string newName = EditorGUILayout.TextField("Server Name", _serverName);
                if (newName != _serverName)
                {
                    _serverName = newName;
                    _serverManager.ServerName = _serverName;
                }
            }
            EditorGUI.EndDisabledGroup();

            bool newAutoStart = EditorGUILayout.Toggle("Auto Start", _autoStart);
            if (newAutoStart != _autoStart)
            {
                _autoStart = newAutoStart;
            }

            bool newVerbose = EditorGUILayout.Toggle("Verbose Logging", _verboseLogging);
            if (newVerbose != _verboseLogging)
            {
                _verboseLogging = newVerbose;
            }

            EditorGUILayout.Space(5);

            // 启动/停止按钮
            EditorGUILayout.BeginHorizontal();
            {
                Color originalColor = GUI.backgroundColor;

                if (!_serverManager.IsRunning)
                {
                    GUI.backgroundColor = Color.green;
                    if (GUILayout.Button("▶ Start Server", GUILayout.Height(30)))
                    {
                        _serverManager.StartServer();
                    }
                }
                else
                {
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("⏹ Stop Server", GUILayout.Height(30)))
                    {
                        _serverManager.StopServer();
                    }
                }

                GUI.backgroundColor = originalColor;

                if (GUILayout.Button("Clear Logs", GUILayout.Height(30), GUILayout.Width(100)))
                {
                    _serverManager.ClearLogs();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawServerStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 状态指示
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel, GUILayout.Width(60));

                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
                if (_serverManager.IsRunning)
                {
                    statusStyle.normal.textColor = Color.green;
                    EditorGUILayout.LabelField("● Running", statusStyle);
                }
                else
                {
                    statusStyle.normal.textColor = Color.gray;
                    EditorGUILayout.LabelField("○ Stopped", statusStyle);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_serverManager.IsRunning)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Endpoint:", EditorStyles.boldLabel, GUILayout.Width(80));

                string endpoint = $"http://localhost:{_port}";
                EditorGUILayout.SelectableLabel(endpoint, GUILayout.Height(18));

                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = endpoint;
                    Debug.Log($"Copied to clipboard: {endpoint}");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.HelpBox(
                    "Server is running in Editor mode. You can connect MCP clients to the endpoint above.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Server is not running. Click 'Start Server' to begin accepting MCP connections.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawToolsList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showTools = EditorGUILayout.Foldout(_showTools, $"Registered Tools ({_serverManager.GetToolCount()})", true, EditorStyles.foldoutHeader);

            if (_showTools)
            {
                var tools = _serverManager.GetRegisteredTools();

                if (tools.Count == 0)
                {
                    EditorGUILayout.HelpBox("No tools registered", MessageType.Info);
                }
                else
                {
                    _toolsScrollPosition = EditorGUILayout.BeginScrollView(_toolsScrollPosition, GUILayout.MaxHeight(150));

                    foreach (var tool in tools)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"🔧 {tool.Name}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawResourcesList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showResources = EditorGUILayout.Foldout(_showResources, $"Registered Resources ({_serverManager.GetResourceCount()})", true, EditorStyles.foldoutHeader);

            if (_showResources)
            {
                var resources = _serverManager.GetRegisteredResources();

                if (resources.Count == 0)
                {
                    EditorGUILayout.HelpBox("No resources registered", MessageType.Info);
                }
                else
                {
                    foreach (var resource in resources)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        EditorGUILayout.LabelField($"📦 {resource.Name}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"URI: {resource.Uri}", EditorStyles.miniLabel);
                        if (!string.IsNullOrEmpty(resource.Description))
                        {
                            EditorGUILayout.LabelField(resource.Description, EditorStyles.wordWrappedMiniLabel);
                        }
                        EditorGUILayout.EndVertical();
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLogsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _showLogs = EditorGUILayout.Foldout(_showLogs, "Server Logs", true, EditorStyles.foldoutHeader);
            _autoScroll = EditorGUILayout.ToggleLeft("Auto-scroll", _autoScroll, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (_showLogs)
            {
                var logs = _serverManager.GetLogs();

                if (logs.Length == 0)
                {
                    EditorGUILayout.HelpBox("No logs yet", MessageType.Info);
                }
                else
                {
                    if (_autoScroll)
                    {
                        _logScrollPosition.y = Mathf.Infinity;
                    }

                    _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition, GUILayout.MinHeight(100), GUILayout.MaxHeight(200));

                    GUIStyle logStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 10,
                        richText = true
                    };

                    foreach (var log in logs)
                    {
                        string coloredLog = log;
                        if (log.Contains("ERROR"))
                        {
                            coloredLog = $"<color=red>{log}</color>";
                        }
                        else if (log.Contains("Started") || log.Contains("Registered"))
                        {
                            coloredLog = $"<color=green>{log}</color>";
                        }

                        EditorGUILayout.LabelField(coloredLog, logStyle);
                    }

                    EditorGUILayout.EndScrollView();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSeparator()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void LoadPreferences()
        {
            _port = EditorPrefs.GetInt(PREF_PORT, 3000);
            _autoStart = EditorPrefs.GetBool(PREF_AUTO_START, false);
            _serverName = EditorPrefs.GetString(PREF_SERVER_NAME, "Unity MCP Server (Editor)");
            _verboseLogging = EditorPrefs.GetBool(PREF_VERBOSE, true);
        }

        private void SavePreferences()
        {
            EditorPrefs.SetInt(PREF_PORT, _port);
            EditorPrefs.SetBool(PREF_AUTO_START, _autoStart);
            EditorPrefs.SetString(PREF_SERVER_NAME, _serverName);
            EditorPrefs.SetBool(PREF_VERBOSE, _verboseLogging);
        }
    }
}
