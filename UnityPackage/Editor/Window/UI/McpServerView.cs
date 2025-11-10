using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityAIStudio.McpServer.Models;
using UnityAIStudio.McpServer.Editor.Window.Models;
using UnityAIStudio.McpServer.Services;
using UnityEditor;
using UnityEngine;

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
        private int maxLogMessages = 100; // 默认 100，可在 UI 中调整

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
            service.OnToolPackagesListUpdated += OnToolPackagesListUpdated;

            // 载入日志上限（可选持久化）
            maxLogMessages = EditorPrefs.GetInt("UnityAIStudio.McpServer.Logs.MaxLines", 100);

            // 从全局缓冲恢复日志，避免关窗丢失
            var persisted = McpServerManager.GetLogs();
            if (persisted != null && persisted.Count > 0)
            {
                logMessages = new List<string>(persisted);
                TrimLogsToCapacity();
            }
        }

        public void OnDisable()
        {
            // Unsubscribe from events
            service.OnServerStatusChanged -= OnServerStatusChanged;
            service.OnConnectionStatusChanged -= OnConnectionStatusChanged;
            service.OnLogMessage -= OnLogMessage;
            service.OnToolsListUpdated -= OnToolsListUpdated;
            service.OnToolPackagesListUpdated -= OnToolPackagesListUpdated;

            // 保存日志上限
            EditorPrefs.SetInt("UnityAIStudio.McpServer.Logs.MaxLines", maxLogMessages);
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

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("📘 Integration Guide", GUILayout.Width(180), GUILayout.Height(24)))
            {
                UnityAIStudio.McpServer.Docs.IntegrationGuideWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

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
                    if (service.Config.port != parsedPort)
                    {
                        service.Config.port = parsedPort;
                        service.Config.Save(); // 立即持久化，避免编译或重载丢失
                    }
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

            GUILayout.Space(8);

            // Auto-restart after compile setting
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Auto-restart after compile:", EditorStyles.boldLabel, GUILayout.Width(200));
            bool autoRestart = McpServerManager.GetAutoRestartAfterCompile();
            bool newAutoRestart = EditorGUILayout.Toggle(autoRestart, GUILayout.Width(20));
            if (newAutoRestart != autoRestart)
            {
                McpServerManager.SetAutoRestartAfterCompile(newAutoRestart);
            }

            var hintStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            };
            EditorGUILayout.LabelField("(Automatically restart server after script compilation)", hintStyle);
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
            showTools = EditorGUILayout.Foldout(showTools, "📦 Tool Packages", true, McpUIStyles.FoldoutStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(90), GUILayout.Height(22)))
            {
                service.RefreshToolPackages();
            }
            EditorGUILayout.EndHorizontal();

            if (!showTools) return;

            EditorGUILayout.BeginVertical(McpUIStyles.StatusBoxStyle);

            // Info message
            EditorGUILayout.HelpBox(
                "Tool packages control which tools are available to MCP clients. " +
                "Disabled packages and their tools will not be discovered by the server. " +
                "Restart the server after making changes.",
                MessageType.Info
            );

            GUILayout.Space(5);

            // Search filter
            DrawToolPackagesFilter();

            GUILayout.Space(8);

            // Tool packages list
            DrawToolPackagesList();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolPackagesFilter()
        {
            EditorGUILayout.BeginVertical(McpUIStyles.CardStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 Search:", EditorStyles.boldLabel, GUILayout.Width(80));
            toolSearchFilter = EditorGUILayout.TextField(toolSearchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawToolPackagesList()
        {
            var toolPackages = service.GetAvailableToolPackages();
            var filteredPackages = toolPackages.Where(p =>
                string.IsNullOrEmpty(toolSearchFilter)
                || (p.displayName != null && p.displayName.ToLower().Contains(toolSearchFilter.ToLower()))
                || (p.description != null && p.description.ToLower().Contains(toolSearchFilter.ToLower()))
                || (p.category != null && p.category.ToLower().Contains(toolSearchFilter.ToLower()))
            ).ToList();

            var enabledCount = toolPackages.Count(p => p.enabled);
            var disabledCount = toolPackages.Count - enabledCount;

            var countStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };
            EditorGUILayout.LabelField(
                $"Showing {filteredPackages.Count} of {toolPackages.Count} packages ({enabledCount} enabled, {disabledCount} disabled)",
                countStyle
            );

            GUILayout.Space(5);

            toolsScrollPosition = EditorGUILayout.BeginScrollView(toolsScrollPosition, GUILayout.Height(250));

            foreach (var package in filteredPackages)
            {
                DrawToolPackageItem(package);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolPackageItem(McpToolPackage package)
        {
            var boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };

            // Visual feedback for disabled packages
            var originalBgColor = GUI.backgroundColor;
            if (!package.enabled)
            {
                GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            }

            EditorGUILayout.BeginHorizontal(boxStyle);
            GUI.backgroundColor = originalBgColor;

            // Enable/Disable toggle
            bool newEnabled = EditorGUILayout.Toggle(package.enabled, GUILayout.Width(20));
            if (newEnabled != package.enabled)
            {
                service.SetToolPackageEnabled(package.className, newEnabled);

                // Show restart reminder
                string action = newEnabled ? "enabled" : "disabled";
                string message = $"Tool package '{package.category}' {action}.\n\n" +
                                $"The {package.toolCount} tools in this package will be {action}.\n\n" +
                                $"⚠️ Please restart the MCP Server for changes to take effect.";

                if (service.State.Status == ServerStatus.Running)
                {
                    bool shouldRestart = EditorUtility.DisplayDialog(
                        "Tool Package Changed",
                        message + "\n\nWould you like to restart the server now?",
                        "Restart Now",
                        "Later"
                    );

                    if (shouldRestart)
                    {
                        service.Restart();
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Tool Package Changed",
                        message,
                        "OK"
                    );
                }
            }

            EditorGUILayout.BeginVertical();

            // Category name as main title and tool count
            EditorGUILayout.BeginHorizontal();
            var nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = package.enabled ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
            };
            EditorGUILayout.LabelField(package.category, nameStyle);

            var countBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = McpUIStyles.InfoColor },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"({package.toolCount} tools)", countBadgeStyle, GUILayout.Width(70));

            // Disabled badge
            if (!package.enabled)
            {
                var disabledBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = McpUIStyles.WarningColor },
                    fontStyle = FontStyle.Bold,
                    fontSize = 10
                };
                EditorGUILayout.LabelField("⊗ DISABLED", disabledBadgeStyle, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();

            // Class name as subtitle
            var classNameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.7f, 0.8f) },
                fontSize = 10
            };
            EditorGUILayout.LabelField($"Class: {package.displayName}", classNameStyle);

            // Description
            if (!string.IsNullOrEmpty(package.description))
            {
                var descStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
                    wordWrap = true,
                    fontSize = 10
                };
                EditorGUILayout.LabelField(package.description, descStyle);
            }

            // Show tool names in a smaller font
            if (package.toolNames != null && package.toolNames.Count > 0)
            {
                var toolNamesText = string.Join(", ", package.toolNames);
                var toolNamesStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.6f, 0.65f) },
                    fontSize = 9,
                    wordWrap = true,
                    fontStyle = FontStyle.Italic
                };
                EditorGUILayout.LabelField($"Tools: {toolNamesText}", toolNamesStyle);
            }

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Logs Section

        private void DrawLogsSection()
        {
            EditorGUILayout.BeginHorizontal();
            showLogs = EditorGUILayout.Foldout(showLogs, $"📝 Logs ({logMessages.Count})", true, McpUIStyles.FoldoutStyle);
            GUILayout.FlexibleSpace();

            // 日志最大行设置
            EditorGUILayout.LabelField("Max:", EditorStyles.miniLabel, GUILayout.Width(35));
            int newCap = EditorGUILayout.IntField(maxLogMessages, GUILayout.Width(60));
            newCap = Mathf.Clamp(newCap, 10, 10000);
            if (newCap != maxLogMessages)
            {
                maxLogMessages = newCap;
                TrimLogsToCapacity();
                // 同步全局日志容量，持久化并裁剪全局缓冲
                McpServerManager.SetLogCapacity(maxLogMessages);
            }

            // 导出按钮
            if (GUILayout.Button("⬇ Export", GUILayout.Width(90), GUILayout.Height(22)))
            {
                ExportLogs();
            }

            // 清空按钮
            if (GUILayout.Button("🗑️ Clear", GUILayout.Width(90), GUILayout.Height(22)))
            {
                logMessages.Clear();
                McpServerManager.ClearLogs();
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
            TrimLogsToCapacity();
        }

        private void OnToolsListUpdated(List<McpTool> tools)
        {
            Debug.Log($"Tools list updated: {tools.Count} tools available");
        }

        private void OnToolPackagesListUpdated(List<McpToolPackage> toolPackages)
        {
            Debug.Log($"Tool packages list updated: {toolPackages.Count} packages available");
        }

        #endregion

        #region Helpers

        private void TrimLogsToCapacity()
        {
            while (logMessages.Count > maxLogMessages)
            {
                logMessages.RemoveAt(0);
            }
        }

        private void ExportLogs()
        {
            var path = EditorUtility.SaveFilePanel(
                "Export MCP Server Logs",
                Application.dataPath,
                "mcp_server_logs.txt",
                "txt");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    File.WriteAllLines(path, logMessages);
                    Debug.Log($"Logs exported to: {path}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to export logs: {ex.Message}");
                }
            }
        }

        #endregion
    }
}
