﻿using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;

namespace MapleModelContextProtocol.Server
{
    public abstract partial class McpServer
    {
        public abstract ClientCapabilities ClientCapabilities { get; }

        public abstract Implementation ClientInfo { get; }
        public abstract McpServerOptions ServerOptions { get; }

        public abstract LoggingLevel? LoggingLevel { get; }

        public abstract Task RunAsync(CancellationToken cancellationToken = default);
    }
}