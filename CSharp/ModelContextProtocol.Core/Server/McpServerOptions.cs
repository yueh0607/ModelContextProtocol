using System;
using System.Collections.Generic;
using ModelContextProtocol.Protocol;
namespace ModelContextProtocol.Server
{

    public sealed class McpServerOptions
    {
        private McpServerHandlers _handlers;

        public Implementation ServerInfo { get; set; }

        public ServerCapabilities Capabilities { get; set; }

        public McpServerHandlers Handlers
        {
            get => _handlers ?? new McpServerHandlers();
            set => _handlers = value ?? throw new ArgumentNullException(nameof(value));
        }
        public string ProtocolVersion { get; set; }

        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public IList<SimpleMcpServerTool> ToolCollection { get; set; } = new List<SimpleMcpServerTool>();
    }
}