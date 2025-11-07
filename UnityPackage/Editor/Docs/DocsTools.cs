using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using UnityAIStudio.McpServer.Tools;
using UnityAIStudio.McpServer.Tools.Attributes;

namespace UnityAIStudio.McpServer.Docs
{
    [McpToolClass(Category = "Docs", Description = "Documentation and integration helpers")]
    public class DocsTools
    {
        [McpTool(Description = "Open the MCP Integration Guide window", Category = "Docs")]
        public async Task<CallToolResult> OpenIntegrationGuide(CancellationToken ct = default)
        {
            return await UnityMainThreadScheduler.ExecuteAsync(() =>
            {
                IntegrationGuideWindow.ShowWindow();
                return McpUtils.Success("Opened MCP Integration Guide window");
            });
        }
    }
}


