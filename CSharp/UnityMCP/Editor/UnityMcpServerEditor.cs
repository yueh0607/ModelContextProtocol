using UnityEditor;
using UnityEngine;

namespace UnityMCP.Editor
{
    /// <summary>
    /// UnityMcpServer组件的自定义Inspector
    /// 提供更好的可视化和控制
    /// </summary>
    [CustomEditor(typeof(UnityMcpServer))]
    public class UnityMcpServerEditor : UnityEditor.Editor
    {
        private UnityMcpServer _target;
        private Vector2 _logScrollPosition;
        private bool _showLogs = true;

        private void OnEnable()
        {
            _target = (UnityMcpServer)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            DrawStatus();
            EditorGUILayout.Space(10);

            // 绘制默认的序列化字段
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            DrawControls();
            DrawEditorModeHint();
            DrawLogs();

            serializedObject.ApplyModifiedProperties();

            // 持续重绘以更新状态
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🚀 Unity MCP Server (Runtime)", titleStyle);
            DrawSeparator();
        }

        private void DrawStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Server runs only in Play mode. For Editor mode, use Window → MCP Server",
                    MessageType.Info);
            }
            else
            {
                GUIStyle statusStyle = new GUIStyle(EditorStyles.label);

                if (_target.IsRunning)
                {
                    statusStyle.normal.textColor = Color.green;
                    EditorGUILayout.LabelField("● Running", statusStyle);

                    SerializedProperty portProp = serializedObject.FindProperty("port");
                    EditorGUILayout.LabelField($"Endpoint: http://localhost:{portProp.intValue}");
                }
                else
                {
                    statusStyle.normal.textColor = Color.gray;
                    EditorGUILayout.LabelField("○ Stopped", statusStyle);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawControls()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            Color originalColor = GUI.backgroundColor;

            if (!_target.IsRunning)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("▶ Start Server", GUILayout.Height(25)))
                {
                    _target.StartServer();
                }
            }
            else
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("⏹ Stop Server", GUILayout.Height(25)))
                {
                    _target.StopServer();
                }
            }

            GUI.backgroundColor = originalColor;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawEditorModeHint()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("💡 Tip", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "For running MCP Server in Editor mode (without Play), open:\n" +
                "Window → MCP Server\n\n" +
                "Editor mode allows the server to run independently of your game runtime.",
                MessageType.Info);

            if (GUILayout.Button("Open Editor Window"))
            {
                McpServerEditorWindow.ShowWindow();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLogs()
        {
            if (!Application.isPlaying) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _showLogs = EditorGUILayout.Foldout(_showLogs, "Recent Logs", true, EditorStyles.foldoutHeader);

            if (_showLogs)
            {
                var logs = _target.GetRecentLogs();

                if (logs.Length == 0)
                {
                    EditorGUILayout.HelpBox("No logs yet", MessageType.Info);
                }
                else
                {
                    _logScrollPosition = EditorGUILayout.BeginScrollView(_logScrollPosition,
                        GUILayout.MaxHeight(150));

                    GUIStyle logStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        fontSize = 9
                    };

                    foreach (var log in logs)
                    {
                        EditorGUILayout.LabelField(log, logStyle);
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
    }
}
