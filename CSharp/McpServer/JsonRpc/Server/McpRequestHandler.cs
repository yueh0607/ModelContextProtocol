using System.Threading;
using System.Threading.Tasks;

namespace JsonRpc.Server
{
    public delegate ValueTask<TResult> McpRequestHandler<TParams, TResult>(
        RequestContext<TParams> request,
        CancellationToken cancellationToken);
}