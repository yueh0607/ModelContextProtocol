using System.Threading;
using System.Threading.Tasks;

namespace MapleModelContextProtocol.Server.Transport
{

    public interface IMcpTransport
    {

        Task<string> ReadMessageAsync(CancellationToken cancellationToken);

        Task WriteMessageAsync(string message, CancellationToken cancellationToken);

        Task StartAsync(CancellationToken cancellationToken);

        void Stop();
    }
}

