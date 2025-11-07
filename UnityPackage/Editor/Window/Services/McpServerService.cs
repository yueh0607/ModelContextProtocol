using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using UnityAIStudio.McpServer.Models;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Services
{
    /// <summary>
    /// MCP Server service implementation
    /// </summary>
    public class McpServerService : IMcpServerService
    {
        // Events
        public event Action<ServerStatus> OnServerStatusChanged;
        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        public event Action<string> OnLogMessage;
        public event Action<List<McpTool>> OnToolsListUpdated;

        // State
        public ServerState State { get; private set; }
        public ServerConfig Config { get; private set; }

        // Private fields
        private Process serverProcess;
        private List<McpTool> availableTools;
        private float lastUpdateTime;

        public McpServerService()
        {
            State = new ServerState();
            Config = ServerConfig.Load();
            InitializeTools();
        }

        #region Server Control

        public void Start(int port)
        {
            if (State.Status == ServerStatus.Running || State.Status == ServerStatus.Starting)
            {
                Log("Server is already running or starting");
                return;
            }

            Config.port = port;
            Config.Save();

            State.CurrentPort = port;
            SetServerStatus(ServerStatus.Starting);
            Log($"Starting MCP Server on port {port}...");

            try
            {
                // TODO: Implement actual server start logic
                UnityEngine.Debug.Log($"[MCP Server] Starting on port {port}");

                State.StartTime = DateTime.Now;

                // Simulate successful start after a delay
                EditorApplication.delayCall += () =>
                {
                    SetServerStatus(ServerStatus.Running);
                    SetConnectionStatus(ConnectionStatus.Connected);
                    Log($"Server started successfully on port {port}");
                };
            }
            catch (Exception ex)
            {
                SetServerStatus(ServerStatus.Error);
                State.ErrorMessage = ex.Message;
                Log($"Failed to start server: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (State.Status == ServerStatus.Stopped || State.Status == ServerStatus.Stopping)
            {
                Log("Server is already stopped or stopping");
                return;
            }

            SetServerStatus(ServerStatus.Stopping);
            Log("Stopping MCP Server...");

            try
            {
                // TODO: Implement actual server stop logic
                if (serverProcess != null && !serverProcess.HasExited)
                {
                    serverProcess.Kill();
                    serverProcess.Dispose();
                    serverProcess = null;
                }

                State.Reset();
                SetServerStatus(ServerStatus.Stopped);
                SetConnectionStatus(ConnectionStatus.Disconnected);
                Log("Server stopped successfully");
            }
            catch (Exception ex)
            {
                SetServerStatus(ServerStatus.Error);
                State.ErrorMessage = ex.Message;
                Log($"Failed to stop server: {ex.Message}");
            }
        }

        public void Restart()
        {
            Log("Restarting server...");
            int port = State.CurrentPort;
            Stop();
            EditorApplication.delayCall += () => Start(port);
        }

        #endregion

        #region Tools Management

        private void InitializeTools()
        {
            availableTools = new List<McpTool>
            {
                new McpTool("GetGameObject", "Get GameObject by name or path", "Scene"),
                new McpTool("CreateGameObject", "Create a new GameObject in the scene", "Scene"),
                new McpTool("DestroyGameObject", "Destroy a GameObject", "Scene"),
                new McpTool("GetComponent", "Get component from GameObject", "Component"),
                new McpTool("AddComponent", "Add component to GameObject", "Component"),
                new McpTool("RemoveComponent", "Remove component from GameObject", "Component"),
                new McpTool("SetPosition", "Set GameObject position", "Transform"),
                new McpTool("SetRotation", "Set GameObject rotation", "Transform"),
                new McpTool("SetScale", "Set GameObject scale", "Transform"),
                new McpTool("LoadScene", "Load a Unity scene", "Scene"),
                new McpTool("SaveScene", "Save current scene", "Scene"),
                new McpTool("PlayMode", "Enter/Exit play mode", "Editor"),
                new McpTool("CompileScripts", "Trigger script compilation", "Editor"),
                new McpTool("GetProjectInfo", "Get Unity project information", "Project"),
                new McpTool("ListAssets", "List assets in project", "Assets"),
                new McpTool("ImportAsset", "Import asset into project", "Assets"),
                new McpTool("ExportPackage", "Export Unity package", "Assets"),
                new McpTool("CreateMaterial", "Create a new material", "Assets"),
                new McpTool("CreatePrefab", "Create a prefab from GameObject", "Assets"),
                new McpTool("ExecuteMenuItem", "Execute Unity menu item", "Editor")
            };
        }

        public List<McpTool> GetAvailableTools()
        {
            return new List<McpTool>(availableTools);
        }

        public void RefreshTools()
        {
            Log("Refreshing tools list...");
            // TODO: Query the server for available tools
            OnToolsListUpdated?.Invoke(availableTools);
            Log($"Found {availableTools.Count} available tools");
        }

        public void SetToolEnabled(string toolName, bool enabled)
        {
            var tool = availableTools.Find(t => t.name == toolName);
            if (tool != null)
            {
                tool.enabled = enabled;
                Log($"Tool '{toolName}' {(enabled ? "enabled" : "disabled")}");
            }
        }

        #endregion

        #region Network Utilities

        public bool IsPortAvailable(int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public int FindAvailablePort(int startPort = 8080)
        {
            for (int port = startPort; port < startPort + 100; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1;
        }

        #endregion

        #region Lifecycle

        public void Update()
        {
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastUpdateTime > 1.0f) // Update every second
            {
                lastUpdateTime = currentTime;

                if (State.Status == ServerStatus.Running)
                {
                    // TODO: Update actual server stats
                    // Simulate random client connections for demo
                    if (UnityEngine.Random.value > 0.9f)
                    {
                        State.ConnectedClients = UnityEngine.Random.Range(0, 5);
                    }
                }
            }
        }

        public void Dispose()
        {
            if (State.Status == ServerStatus.Running)
            {
                Stop();
            }
        }

        #endregion

        #region Private Methods

        private void SetServerStatus(ServerStatus status)
        {
            if (State.Status != status)
            {
                State.Status = status;
                OnServerStatusChanged?.Invoke(status);
            }
        }

        private void SetConnectionStatus(ConnectionStatus status)
        {
            if (State.ConnectionStatus != status)
            {
                State.ConnectionStatus = status;
                OnConnectionStatusChanged?.Invoke(status);
            }
        }

        private void Log(string message)
        {
            UnityEngine.Debug.Log($"[MCP Server] {message}");
            OnLogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        #endregion
    }
}
