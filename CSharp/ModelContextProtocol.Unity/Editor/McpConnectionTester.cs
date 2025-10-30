using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ModelContextProtocol.Unity.Editor
{
    /// <summary>
    /// MCP 连接测试工具 - 在 Unity Editor 内测试 MCP 服务器连接
    /// </summary>
    public class McpConnectionTester : EditorWindow
    {
        private string _serverUrl = "http://127.0.0.1:8767";
        private string _testResult = "";
        private Vector2 _scrollPosition;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        [MenuItem("MCP Server/Connection Tester")]
        public static void ShowWindow()
        {
            var window = GetWindow<McpConnectionTester>("MCP Tester");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MCP Server Connection Tester", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _serverUrl = EditorGUILayout.TextField("Server URL", _serverUrl);
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Test tools/list", GUILayout.Height(30)))
                {
                    TestToolsList();
                }

                if (GUILayout.Button("Test echo Tool", GUILayout.Height(30)))
                {
                    TestEchoTool();
                }
            }

            if (GUILayout.Button("Clear Result", GUILayout.Height(25)))
            {
                _testResult = "";
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Result:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_testResult, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private async void TestToolsList()
        {
            _testResult = "Testing tools/list...\n";
            Repaint();

            try
            {
                var json = @"{""jsonrpc"":""2.0"",""id"":""1"",""method"":""tools/list"",""params"":{}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _testResult += $"Sending to: {_serverUrl}\n";
                _testResult += $"Request: {json}\n\n";
                Repaint();

                var response = await _httpClient.PostAsync(_serverUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _testResult += $"Status: {(int)response.StatusCode} {response.StatusCode}\n";
                _testResult += $"Response: {responseBody}\n";
                _testResult += "\n✅ Test completed successfully!";
            }
            catch (Exception ex)
            {
                _testResult += $"\n❌ Error: {ex.Message}\n";
                _testResult += $"Type: {ex.GetType().Name}\n";
                if (ex.InnerException != null)
                {
                    _testResult += $"Inner: {ex.InnerException.Message}\n";
                }
            }

            Repaint();
        }

        private async void TestEchoTool()
        {
            _testResult = "Testing echo tool...\n";
            Repaint();

            try
            {
                var json = @"{""jsonrpc"":""2.0"",""id"":""2"",""method"":""tools/call"",""params"":{""name"":""echo"",""arguments"":{""message"":""Hello from Unity!""}}}";
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _testResult += $"Sending to: {_serverUrl}\n";
                _testResult += $"Request: {json}\n\n";
                Repaint();

                var response = await _httpClient.PostAsync(_serverUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _testResult += $"Status: {(int)response.StatusCode} {response.StatusCode}\n";
                _testResult += $"Response: {responseBody}\n";
                _testResult += "\n✅ Test completed successfully!";
            }
            catch (Exception ex)
            {
                _testResult += $"\n❌ Error: {ex.Message}\n";
                _testResult += $"Type: {ex.GetType().Name}\n";
                if (ex.InnerException != null)
                {
                    _testResult += $"Inner: {ex.InnerException.Message}\n";
                }
            }

            Repaint();
        }
    }
}

