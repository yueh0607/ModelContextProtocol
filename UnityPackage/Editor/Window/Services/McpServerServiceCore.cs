using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Server.Transport;
using UnityAIStudio.McpServer.Models;
using UnityAIStudio.McpServer.Editor.Window.Models;
using UnityAIStudio.McpServer.Tools;
using UnityEditor;
using UnityEngine;

namespace UnityAIStudio.McpServer.Services
{
    /// <summary>
    /// MCP Server service implementation - 基于Core库的适配器
    /// </summary>
    public class McpServerServiceCore : IMcpServerService
    {
        // Events
        public event Action<ServerStatus> OnServerStatusChanged;
        public event Action<ConnectionStatus> OnConnectionStatusChanged;
        public event Action<string> OnLogMessage;
        public event Action<List<McpTool>> OnToolsListUpdated;
        public event Action<List<McpToolPackage>> OnToolPackagesListUpdated;

        // State
        public ServerState State { get; private set; }
        public ServerConfig Config { get; private set; }

        // Core MCP Server
        private TransportBasedMcpServer coreServer;
        private CancellationTokenSource cancellationTokenSource;
        private Task serverTask;
        private HttpTransport transport;

        // Tools
        private List<McpTool> availableTools;
        private List<SimpleMcpServerTool> coreTools;
        private List<McpToolPackage> availableToolPackages;

        // Update tracking
        private float lastUpdateTime;

        public McpServerServiceCore()
        {
            State = new ServerState();
            Config = ServerConfig.Load();
            InitializeTools();

            // 初始化主线程调度器
            UnityMainThreadScheduler.Initialize();
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
                // 1. 创建HTTP Transport
                transport = new HttpTransport(port, "/", LogFromCore);

                // 2. 创建Server Options
                var options = CreateServerOptions();

                // 3. 添加Unity工具
                RegisterTools(options);

                // 4. 创建Core服务器
                coreServer = new TransportBasedMcpServer(transport, options);

                // 5. 启动服务器（在后台线程）
                cancellationTokenSource = new CancellationTokenSource();
                serverTask = Task.Run(async () =>
                {
                    try
                    {
                        await coreServer.RunAsync(cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                    }
                    catch (Exception ex)
                    {
                        UnityMainThreadScheduler.Execute(() =>
                        {
                            SetServerStatus(ServerStatus.Error);
                            State.ErrorMessage = ex.Message;
                            Log($"Server error: {ex.Message}");
                        });
                    }
                }, cancellationTokenSource.Token);

                // 6. 更新状态
                State.StartTime = DateTime.Now;
                SetServerStatus(ServerStatus.Running);
                SetConnectionStatus(ConnectionStatus.Connected);
                Log($"Server started successfully on port {port}");
                Log($"Server URL: http://localhost:{port}/");
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
                // 1. 取消服务器任务
                cancellationTokenSource?.Cancel();

                // 2. 停止Transport
                transport?.Stop();

                // 3. 等待服务器任务完成（超时5秒）
                if (serverTask != null && !serverTask.IsCompleted)
                {
                    serverTask.Wait(TimeSpan.FromSeconds(5));
                }

                // 4. 清理资源
                transport?.Dispose();
                cancellationTokenSource?.Dispose();

                transport = null;
                coreServer = null;
                serverTask = null;
                cancellationTokenSource = null;

                // 5. 重置状态
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

            // 延迟启动，确保端口释放
            EditorApplication.delayCall += () =>
            {
                System.Threading.Thread.Sleep(500);
                Start(port);
            };
        }

        #endregion

        #region Tools Management

        private void InitializeTools()
        {
            // 初始为空；在注册工具后由 coreTools 动态构建
            availableTools = new List<McpTool>();
            availableToolPackages = new List<McpToolPackage>();
        }

        private McpServerOptions CreateServerOptions()
        {
            return new McpServerOptions
            {
                ServerInfo = new Implementation
                {
                    Name = "Unity MCP Server",
                    Version = Config.version
                },
                Capabilities = new ServerCapabilities
                {
                    Tools = new ToolsCapability()
                }
            };
        }

        private void RegisterTools(McpServerOptions options)
        {
            // 首先发现所有ToolPackage并加载启用状态
            availableToolPackages = McpToolDiscovery.DiscoverAllToolPackages();

            // 获取启用的ToolPackage类名集合
            var enabledToolPackages = new HashSet<string>(
                availableToolPackages.Where(p => p.enabled).Select(p => p.className)
            );

            // 只注册启用的ToolPackage中的工具
            coreTools = McpToolDiscovery.DiscoverAllToolsWithFilter(enabledToolPackages);

            // 注册到服务器选项
            options.ToolCollection = coreTools;

            // 同步构建 UI 显示用的工具列表（名称 + 描述）
            RebuildAvailableToolsFromCore();
            OnToolsListUpdated?.Invoke(availableTools);
            OnToolPackagesListUpdated?.Invoke(availableToolPackages);

            Log($"Registered {coreTools.Count} MCP tools from {enabledToolPackages.Count} enabled packages");
        }

        public List<McpTool> GetAvailableTools()
        {
            return new List<McpTool>(availableTools);
        }

        public void RefreshTools()
        {
            Log("Refreshing tools list...");
            // 从 coreTools 重新构建（允许域重载后刷新）
            RebuildAvailableToolsFromCore();
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

        public List<McpToolPackage> GetAvailableToolPackages()
        {
            return new List<McpToolPackage>(availableToolPackages ?? new List<McpToolPackage>());
        }

        public void RefreshToolPackages()
        {
            Log("Refreshing tool packages list...");
            // 重新发现ToolPackage
            availableToolPackages = McpToolDiscovery.DiscoverAllToolPackages();
            OnToolPackagesListUpdated?.Invoke(availableToolPackages);
            Log($"Found {availableToolPackages.Count} available tool packages");
        }

        public void SetToolPackageEnabled(string className, bool enabled)
        {
            var toolPackage = availableToolPackages?.Find(p => p.className == className);
            if (toolPackage != null)
            {
                toolPackage.enabled = enabled;
                // 保存到EditorPrefs
                UnityEditor.EditorPrefs.SetBool(toolPackage.GetPrefsKey(), enabled);

                string action = enabled ? "enabled" : "disabled";
                Log($"Tool package '{toolPackage.displayName}' ({toolPackage.category}) {action}");
                Log($"⚠️ Restart the server to apply changes. {toolPackage.toolCount} tools will be {action}.");

                // 通知UI更新
                OnToolPackagesListUpdated?.Invoke(availableToolPackages);
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

                // 检查服务器任务状态
                if (State.Status == ServerStatus.Running && serverTask != null)
                {
                    if (serverTask.IsFaulted)
                    {
                        SetServerStatus(ServerStatus.Error);
                        State.ErrorMessage = serverTask.Exception?.GetBaseException().Message ?? "Unknown error";
                        Log($"Server faulted: {State.ErrorMessage}");
                    }
                    else if (serverTask.IsCompleted && !serverTask.IsCanceled)
                    {
                        SetServerStatus(ServerStatus.Stopped);
                        SetConnectionStatus(ConnectionStatus.Disconnected);
                        Log("Server stopped unexpectedly");
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

            UnityMainThreadScheduler.Cleanup();
        }

        #endregion

        #region Private Methods

        private void RebuildAvailableToolsFromCore()
        {
            var newList = new List<McpTool>();
            if (coreTools != null)
            {
                foreach (var t in coreTools)
                {
                    var meta = t.ProtocolTool;
                    var name = meta?.Name ?? "(Unnamed Tool)";
                    var desc = meta?.Description ?? string.Empty;
                    newList.Add(new McpTool(name, desc));
                }
            }
            availableTools = newList;
        }

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

        private void LogFromCore(string message)
        {
            // 从Core传来的日志，转发到Unity
            UnityMainThreadScheduler.Execute(() => Log(message));
        }

        #endregion
    }
}
