using System.Threading.Tasks;
using System.Threading;
using MapleModelContextProtocol.Protocol;

namespace MapleModelContextProtocol.Server
{
    public delegate ValueTask<TResult> McpRequestHandler<TParams, TResult>(RequestContext<TParams> request, CancellationToken cancellationToken);
    public sealed class McpServerHandlers
    {

        public McpRequestHandler<ListToolsRequestParams, ListToolsResult> ListToolsHandler { get; set; }


        public McpRequestHandler<CallToolRequestParams, CallToolResult> CallToolHandler { get; set; }


        public McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> ListPromptsHandler { get; set; }


        public McpRequestHandler<GetPromptRequestParams, GetPromptResult> GetPromptHandler { get; set; }

        public McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> ListResourcesHandler { get; set; }

        public McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> ReadResourceHandler { get; set; }


    }
}