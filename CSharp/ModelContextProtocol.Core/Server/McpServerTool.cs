using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;

namespace MapleModelContextProtocol.Server
{

    public abstract class McpServerTool : IMcpServerPrimitive
    {
        protected McpServerTool()
        {
        }

        public abstract Tool ProtocolTool { get; }


        public abstract IReadOnlyList<object> Metadata { get; }

        public abstract ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default);

        public override string ToString() => ProtocolTool.Name;

        string IMcpServerPrimitive.Id => ProtocolTool.Name;
    }
}