using System.Collections.Generic;
namespace ModelContextProtocol.Server
{

    public interface IMcpServerPrimitive
    {
        string Id { get; }

        IReadOnlyList<object> Metadata { get; }

    }
}