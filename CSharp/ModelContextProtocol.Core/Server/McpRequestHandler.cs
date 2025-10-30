using System.Threading;
using System.Threading.Tasks;

namespace MapleModelContextProtocol.Server
{
    public delegate ValueTask<TResult> McpRequestHandler<TParams, TResult>(
        RequestContext<TParams> request,
        CancellationToken cancellationToken);
}