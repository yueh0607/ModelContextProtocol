using System;
using MapleModelContextProtocol.Protocol;

namespace MapleModelContextProtocol.Client
{

    public sealed class McpClientOptions
    {
        private McpClientHandlers _handlers;

        public Implementation ClientInfo { get; set; }

        public ClientCapabilities Capabilities { get; set; }

        public string ProtocolVersion { get; set; }

        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public McpClientHandlers Handlers
        {
            get => _handlers ?? (_handlers = new McpClientHandlers());
            set => _handlers = value ?? throw new ArgumentNullException("value");
        }
    }
}