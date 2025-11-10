using System;
using System.Collections.Generic;
using UnityAIStudio.McpServer.Models;
using UnityAIStudio.McpServer.Editor.Window.Models;

namespace UnityAIStudio.McpServer.Services
{
    /// <summary>
    /// Interface for MCP Server service
    /// </summary>
    public interface IMcpServerService
    {
        // Events
        event Action<ServerStatus> OnServerStatusChanged;
        event Action<ConnectionStatus> OnConnectionStatusChanged;
        event Action<string> OnLogMessage;
        event Action<List<McpTool>> OnToolsListUpdated;
        event Action<List<McpToolPackage>> OnToolPackagesListUpdated;

        // State
        ServerState State { get; }
        ServerConfig Config { get; }

        // Server control
        void Start(int port);
        void Stop();
        void Restart();

        // Tools management (legacy - for individual tools)
        List<McpTool> GetAvailableTools();
        void RefreshTools();
        void SetToolEnabled(string toolName, bool enabled);

        // Tool Packages management (new - for package-level control)
        List<McpToolPackage> GetAvailableToolPackages();
        void RefreshToolPackages();
        void SetToolPackageEnabled(string className, bool enabled);

        // Network utilities
        bool IsPortAvailable(int port);
        int FindAvailablePort(int startPort = 8080);

        // Lifecycle
        void Update();
        void Dispose();
    }
}
