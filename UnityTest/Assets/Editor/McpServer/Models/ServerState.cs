using System;

namespace UnityAIStudio.McpServer.Models
{
    /// <summary>
    /// Server status enumeration
    /// </summary>
    public enum ServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }

    /// <summary>
    /// Connection status enumeration
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    /// <summary>
    /// Server runtime state
    /// </summary>
    public class ServerState
    {
        public ServerStatus Status { get; set; } = ServerStatus.Stopped;
        public ConnectionStatus ConnectionStatus { get; set; } = ConnectionStatus.Disconnected;
        public int CurrentPort { get; set; } = 8080;
        public DateTime? StartTime { get; set; }
        public int ConnectedClients { get; set; } = 0;
        public string ErrorMessage { get; set; }

        public TimeSpan? Uptime
        {
            get
            {
                if (StartTime.HasValue && Status == ServerStatus.Running)
                {
                    return DateTime.Now - StartTime.Value;
                }
                return null;
            }
        }

        public bool IsRunning => Status == ServerStatus.Running;
        public bool IsStopped => Status == ServerStatus.Stopped;

        public void Reset()
        {
            Status = ServerStatus.Stopped;
            ConnectionStatus = ConnectionStatus.Disconnected;
            StartTime = null;
            ConnectedClients = 0;
            ErrorMessage = null;
        }
    }
}
