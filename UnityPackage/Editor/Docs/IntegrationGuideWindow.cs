using UnityEngine;
using UnityEditor;
using UnityAIStudio.McpServer.Services;
using UnityAIStudio.McpServer.UI;
using System.Text;
using System.IO;
using ModelContextProtocol.Json;
using ModelContextProtocol.Json.Linq;

namespace UnityAIStudio.McpServer.Docs
{
	public class IntegrationGuideWindow : EditorWindow
	{
		private int selectedTab = 0; // 0: Cursor, 1: Claude Code
		private Vector2 scroll;

		[MenuItem("Window/MCP Integration Guide")]
		public static void ShowWindow()
		{
			var win = GetWindow<IntegrationGuideWindow>("MCP Integration Guide");
			win.minSize = new Vector2(720, 520);
			win.Show();
		}

		private void OnGUI()
		{
			McpUIStyles.Initialize();

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("MCP Integration Guide", McpUIStyles.SectionHeaderStyle);

			EditorGUILayout.BeginVertical(McpUIStyles.StatusBoxStyle);
			var toolbar = new[] { "Cursor", "Claude Code" };
			selectedTab = GUILayout.Toolbar(selectedTab, toolbar, GUILayout.Height(24));
			EditorGUILayout.Space(6);

			scroll = EditorGUILayout.BeginScrollView(scroll);
			if (selectedTab == 0)
			{
				DrawCursorGuide();
			}
			else
			{
				DrawClaudeCodeGuide();
			}
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();
		}

		private string GetServerUrl()
		{
			var service = McpServerManager.GetInstance() ?? McpServerManager.GetOrCreateInstance();
			int port = service?.State?.CurrentPort > 0 ? service.State.CurrentPort : service?.Config?.port ?? 8080;
			return $"http://localhost:{port}/";
		}

		private void DrawCursorGuide()
		{
			string url = GetServerUrl();
			EditorGUILayout.LabelField("Cursor 配置（HTTP 方式）", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("在 Cursor 的 MCP 配置中添加以下条目：", EditorStyles.label);

			string json = "{\n  \"mcpServers\": {\n    \"unity-mcp\": {\n      \"transport\": \"http\",\n      \"url\": \"" + url + "\"\n    }\n  }\n}";
			DrawReadonlyCode(json);

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField("提示", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("- 确保 Unity MCP Server 已启动，并能通过上述 URL 访问。", EditorStyles.miniLabel);
			EditorGUILayout.LabelField("- 若端口不同，请修改 URL。", EditorStyles.miniLabel);

			EditorGUILayout.Space(6);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("✍️ 写入 C\\\\Users\\\\zhenpengyue\\\\.cursor\\\\mcp.json", GUILayout.Width(290), GUILayout.Height(24)))
			{
				WriteCursorConfig(url);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawClaudeCodeGuide()
		{
			string url = GetServerUrl();
			EditorGUILayout.LabelField("Claude Code 配置（HTTP 方式）", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField("在 Claude Code 的 MCP 配置中添加以下条目：", EditorStyles.label);

			string json = "{\n  \"mcpServers\": {\n    \"unity-mcp\": {\n      \"type\": \"http\",\n      \"url\": \"" + url + "\"\n    }\n  }\n}";
			DrawReadonlyCode(json);

			EditorGUILayout.Space(8);
			EditorGUILayout.LabelField("提示", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("- 确保 Unity MCP Server 已启动，并能通过上述 URL 访问。", EditorStyles.miniLabel);
			EditorGUILayout.LabelField("- 部分客户端可能使用不同的键名（如 transport/type），请参考对应客户端文档。", EditorStyles.miniLabel);

			EditorGUILayout.Space(6);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button("✍️ 一键写入（暂不支持）", GUILayout.Width(180), GUILayout.Height(24));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		private void DrawReadonlyCode(string code)
		{
			EditorGUILayout.BeginVertical(McpUIStyles.CardStyle);
			var box = new GUIStyle(EditorStyles.textArea)
			{
				wordWrap = false,
				fontSize = 11
			};
			EditorGUILayout.TextArea(code, box, GUILayout.MinHeight(120));
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("📋 Copy", GUILayout.Width(100)))
			{
				EditorGUIUtility.systemCopyBuffer = code;
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}

		private static readonly string CursorConfigPath = @"C:\\Users\\zhenpengyue\\.cursor\\mcp.json";

		private void WriteCursorConfig(string url)
		{
			try
			{
				var dir = Path.GetDirectoryName(CursorConfigPath);
				if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

				JObject root;
				if (File.Exists(CursorConfigPath))
				{
					var text = File.ReadAllText(CursorConfigPath);
					root = string.IsNullOrWhiteSpace(text) ? new JObject() : JObject.Parse(text);
				}
				else
				{
					root = new JObject();
				}

				if (root["mcpServers"] == null || root["mcpServers"].Type != JTokenType.Object)
				{
					root["mcpServers"] = new JObject();
				}
				var servers = (JObject)root["mcpServers"];
				var unity = new JObject
				{
					["transport"] = "http",
					["url"] = url
				};
				servers["unity-mcp"] = unity;

				File.WriteAllText(CursorConfigPath, root.ToString(Formatting.Indented));
				UnityEngine.Debug.Log($"[MCP Integration] 已写入: {CursorConfigPath}");
			}
			catch (System.Exception ex)
			{
				UnityEngine.Debug.LogError($"[MCP Integration] 写入失败: {ex.Message}");
			}
		}
	}
}


