using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityAIStudio.McpServer.Models;
using UnityAIStudio.McpServer.Services;

namespace UnityAIStudio.McpServer.UI
{
    /// <summary>
    /// Main view for MCP Server UI
    /// </summary>
    public class McpServerView
    {
        private IMcpServerService service;
        private Vector2 scrollPosition;
        private Vector2 toolsScrollPosition;
        private Vector2 logScrollPosition;

        // UI State
        private string portInputString;
        private bool showTools = true;
        private bool showLogs = true;
        private string toolSearchFilter = "";
        private string selectedToolCategory = "All";
        private List<string> logMessages = new List<string>();
        private const int MaxLogMessages = 100;

        public McpServerView(IMcpServerService service)
        {
            this.service = service;
            portInputString = service.Config.port.ToString();
        }

        public void OnEnable()
        {
            // Subscribe to events
            service.OnServerStatusChanged += OnServerStatusChanged;
            service.OnConnectionStatusChanged += OnConnectionStatusChanged;
            service.OnLogMessage += OnLogMessage;
            service.OnToolsListUpdated += OnToolsListUpdated;
        }

        public void OnDisable()
        {
            // Unsubscribe from events
            service.OnServerStatusChanged -= OnServerStatusChanged;
            service.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            service.OnLogMessage -= OnLogMessage;
            service.OnToolsListUpdated -= OnToolsListUpdated;
        }

        public void OnGUI()
        {
            GUILayout.Space(5);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(5);
            DrawServerStatusSection();
            GUILayout.Space(15);
            DrawToolsSection();
            GUILayout.Space(15);
            DrawLogsSection();
            GUILayout.Space(10);

            EditorGUILayout.EndScrollView();
        }

        #region Server Status Section

        private void DrawServerStatusSection()
        {
            EditorGUILayout.LabelField("📊 Server Status & Control", McpUIStyles.SectionHeaderStyle);

            EditorGUILayout.BeginVertical(McpUIStyles.StatusBoxStyle);

            // Status Information
            DrawStatusInformation();

            GUILayout.Space(10);

            // Connection Information
            DrawConnectionInformation();

            GUILayout.Space(12);

            // Control Buttons
            DrawControlButtons();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatusInformation()
        {
            EditorGUILayout.BeginVertical(McpUIStyles.CardStyle);
            EditorGUILayout.LabelField("Status Information", EditorStyles.boldLabel);
            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();

            // Left column
            EditorGUILayout.BeginVertical(GUILayout.Width(400));

            DrawStatusRow("Status:", () => DrawStatusLabel(service.State.Status));
            GUILayout.Space(5);
            DrawStatusRow("Connection:", () => DrawConnectionStatusLabel(service.State.ConnectionStatus));
            GUILayout.Space(5);

            // Port row with edit capability
            DrawPortRow();

            EditorGUILayout.EndVertical();

            // Right column
            EditorGUILayout.BeginVertical();

            DrawStatusRow("Version:", () => EditorGUILayout.LabelField(service.Config.version));

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawPortRow()
        {
            DrawStatusRow("Port:", () =>
            {
                EditorGUI.BeginDisabledGroup(service.State.Status == ServerStatus.Running);
                portInputString = EditorGUILayout.TextField(portInputString, GUILayout.Width(100));

                if (int.TryParse(portInputString, out int parsedPort))
                {
                    service.Config.port = parsedPort;
                }

                // Port availability indicator
                if (service.State.Status != ServerStatus.Running)
                {
                    GUILayout.Space(10);
                    bool isPortAvailable = service.IsPortAvailable(service.Config.port);
                    GUIStyle portStatusStyle = isPortAvailable ?
                        new GUIStyle(EditorStyles.label) { normal = { textColor = McpUIStyles.SuccessColor }, fontSize = 11 } :
                        new GUIStyle(EditorStyles.label) { normal = { textColor = McpUIStyles.ErrorColor }, fontSize = 11 };
                    EditorGUILayout.LabelField(
                        isPortAvailable ? "✓ Available" : "✗ In use",
                        portStatusStyle
                    );
                }
                else
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField($"(Running on {service.State.CurrentPort})", EditorStyles.miniLabel);
                }

                EditorGUI.EndDisabledGroup();
            });
        }

        private void DrawConnectionInformation()
        {
            EditorGUILayout.BeginVertical(McpUIStyles.CardStyle);
            EditorGUILayout.LabelField("Connection Information", EditorStyles.boldLabel);
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server URL:", EditorStyles.boldLabel, GUILayout.Width(120));
            string url = $"http://localhost:{service.State.CurrentPort}";
            EditorGUILayout.SelectableLabel(url, GUILayout.Height(18));
            if (GUILayout.Button("📋 Copy", GUILayout.Width(80), GUILayout.Height(20)))
            {
                EditorGUIUtility.systemCopyBuffer = url;
                Debug.Log("URL copied to clipboard!");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawControlButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (service.State.Status == ServerStatus.Stopped)
            {
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("▶️ Start Server", McpUIStyles.ButtonStyle, GUILayout.Height(45)))
                {
                    service.Start(service.Config.port);
                }
                GUI.backgroundColor = Color.white;
            }
            else if (service.State.Status == ServerStatus.Running)
            {
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("⏹️ Stop Server", McpUIStyles.ButtonStyle, GUILayout.Height(45)))
                {
                    service.Stop();
                }
                GUI.backgroundColor = Color.white;

                GUILayout.Space(10);

                GUI.backgroundColor = new Color(0.9f, 0.7f, 0.2f);
                if (GUILayout.Button("🔄 Restart", McpUIStyles.ButtonStyle, GUILayout.Height(45), GUILayout.Width(150)))
                {
                    service.Restart();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Button(service.State.Status.ToString() + "...", McpUIStyles.ButtonStyle, GUILayout.Height(45));
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusRow(string label, System.Action valueDrawer)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(120));
            valueDrawer?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusLabel(ServerStatus status)
        {
            GUIStyle style = McpUIStyles.StoppedStatusStyle;
            string text = status.ToString();
            string icon = "●";

            switch (status)
            {
                case ServerStatus.Running:
                    style = McpUIStyles.RunningStatusStyle;
                    icon = "●";
                    break;
                case ServerStatus.Error:
                    style = McpUIStyles.ErrorStatusStyle;
                    icon = "✗";
                    break;
                case ServerStatus.Starting:
                case ServerStatus.Stopping:
                    style = McpUIStyles.WarningStatusStyle;
                    icon = "○";
                    break;
            }

            EditorGUILayout.LabelField($"{icon} {text}", style);
        }

        private void DrawConnectionStatusLabel(ConnectionStatus status)
        {
            GUIStyle style = McpUIStyles.StoppedStatusStyle;
            string text = status.ToString();
            string icon = "●";

            switch (status)
            {
                case ConnectionStatus.Connected:
                    style = McpUIStyles.RunningStatusStyle;
                    icon = "✓";
                    break;
                case ConnectionStatus.Error:
                    style = McpUIStyles.ErrorStatusStyle;
                    icon = "✗";
                    break;
                case ConnectionStatus.Connecting:
                    style = McpUIStyles.WarningStatusStyle;
                    icon = "○";
                    break;
            }

            EditorGUILayout.LabelField($"{icon} {text}", style);
        }

        #endregion

        #region Tools Section

        private void DrawToolsSection()
        {
            EditorGUILayout.BeginHorizontal();
            showTools = EditorGUILayout.Foldout(showTools, "🛠️ Available Tools", true, McpUIStyles.FoldoutStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(90), GUILayout.Height(22)))
            {
                service.RefreshTools();
            }
            EditorGUILayout.EndHorizontal();

            if (!showTools) return;

            EditorGUILayout.BeginVertical(McpUIStyles.StatusBoxStyle);

            // Search and filter card
            DrawToolsFilter();

            GUILayout.Space(8);

            // Tools list
            DrawToolsList();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolsFilter()
        {
            EditorGUILayout.BeginVertical(McpUIStyles.CardStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 Search:", EditorStyles.boldLabel, GUILayout.Width(80));
            toolSearchFilter = EditorGUILayout.TextField(toolSearchFilter);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📁 Category:", EditorStyles.boldLabel, GUILayout.Width(80));
            string[] categories = new[] { "All", "Scene", "Component", "Transform", "Editor", "Project", "Assets" };
            selectedToolCategory = categories[EditorGUILayout.Popup(
                System.Array.IndexOf(categories, selectedToolCategory),
                categories
            )];
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolsList()
        {
            var tools = service.GetAvailableTools();
            var filteredTools = tools.Where(t =>
                (string.IsNullOrEmpty(toolSearchFilter) || t.name.ToLower().Contains(toolSearchFilter.ToLower()) ||
                 t.description.ToLower().Contains(toolSearchFilter.ToLower())) &&
                (selectedToolCategory == "All" || t.category == selectedToolCategory)
            ).ToList();

            var toolCountStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField($"Showing {filteredTools.Count} of {tools.Count} tools", toolCountStyle);

            GUILayout.Space(5);

            toolsScrollPosition = EditorGUILayout.BeginScrollView(toolsScrollPosition, GUILayout.Height(200));

            foreach (var tool in filteredTools)
            {
                DrawToolItem(tool);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolItem(McpTool tool)
        {
            var toolBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };

            EditorGUILayout.BeginHorizontal(toolBoxStyle);

            tool.enabled = EditorGUILayout.Toggle(tool.enabled, GUILayout.Width(20));

            EditorGUILayout.BeginVertical();
            var toolNameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField(tool.name, toolNameStyle);

            var descStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                wordWrap = true
            };
            EditorGUILayout.LabelField(tool.description, descStyle);
            EditorGUILayout.EndVertical();

            var categoryStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = McpUIStyles.InfoColor },
                alignment = TextAnchor.MiddleRight
            };
            EditorGUILayout.LabelField($"[{tool.category}]", categoryStyle, GUILayout.Width(90));

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Logs Section

        private void DrawLogsSection()
        {
            EditorGUILayout.BeginHorizontal();
            showLogs = EditorGUILayout.Foldout(showLogs, $"📝 Logs ({logMessages.Count})", true, McpUIStyles.FoldoutStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🗑️ Clear", GUILayout.Width(90), GUILayout.Height(22)))
            {
                logMessages.Clear();
            }
            EditorGUILayout.EndHorizontal();

            if (!showLogs) return;

            EditorGUILayout.BeginVertical(McpUIStyles.StatusBoxStyle);

            var logBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            EditorGUILayout.BeginVertical(logBoxStyle);
            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));

            if (logMessages.Count == 0)
            {
                GUILayout.Space(50);
                var emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 11 };
                EditorGUILayout.LabelField("No logs yet...", emptyStyle);
            }
            else
            {
                for (int i = logMessages.Count - 1; i >= 0; i--)
                {
                    DrawLogMessage(logMessages[i], i);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        private void DrawLogMessage(string message, int index)
        {
            var logStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 10,
                padding = new RectOffset(5, 5, 3, 3),
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };

            if (index % 2 == 0)
            {
                var bgRect = EditorGUILayout.BeginHorizontal();
                EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, bgRect.width, 20), new Color(0.2f, 0.2f, 0.2f, 0.3f));
                EditorGUILayout.LabelField(message, logStyle);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField(message, logStyle);
            }
        }

        #endregion

        #region Event Handlers

        private void OnServerStatusChanged(ServerStatus status)
        {
            Debug.Log($"Server status changed to: {status}");
        }

        private void OnConnectionStatusChanged(ConnectionStatus status)
        {
            Debug.Log($"Connection status changed to: {status}");
        }

        private void OnLogMessage(string message)
        {
            logMessages.Add(message);
            if (logMessages.Count > MaxLogMessages)
            {
                logMessages.RemoveAt(0);
            }
        }

        private void OnToolsListUpdated(List<McpTool> tools)
        {
            Debug.Log($"Tools list updated: {tools.Count} tools available");
        }

        #endregion
    }
}
