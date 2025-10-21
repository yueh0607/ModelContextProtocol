using System;
using UnityEngine;

namespace UnityAIStudio.McpServer.Models
{
    /// <summary>
    /// MCP Server configuration
    /// </summary>
    [Serializable]
    public class ServerConfig
    {
        public int port = 8080;
        public string version = "1.0.0";
        public bool autoStart = false;
        public int maxConnections = 10;

        public ServerConfig()
        {
        }

        public ServerConfig(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// Load configuration from EditorPrefs
        /// </summary>
        public static ServerConfig Load()
        {
            var config = new ServerConfig
            {
                port = UnityEditor.EditorPrefs.GetInt("McpServer_Port", 8080),
                autoStart = UnityEditor.EditorPrefs.GetBool("McpServer_AutoStart", false),
                maxConnections = UnityEditor.EditorPrefs.GetInt("McpServer_MaxConnections", 10)
            };
            return config;
        }

        /// <summary>
        /// Save configuration to EditorPrefs
        /// </summary>
        public void Save()
        {
            UnityEditor.EditorPrefs.SetInt("McpServer_Port", port);
            UnityEditor.EditorPrefs.SetBool("McpServer_AutoStart", autoStart);
            UnityEditor.EditorPrefs.SetInt("McpServer_MaxConnections", maxConnections);
        }

        public ServerConfig Clone()
        {
            return new ServerConfig
            {
                port = this.port,
                version = this.version,
                autoStart = this.autoStart,
                maxConnections = this.maxConnections
            };
        }
    }
}
