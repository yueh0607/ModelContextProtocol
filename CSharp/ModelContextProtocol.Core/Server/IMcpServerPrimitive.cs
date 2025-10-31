using System.Collections.Generic;

namespace MapleModelContextProtocol.Server
{

    public interface IMcpServerPrimitive
    {
        string Id { get; }

        IReadOnlyList<object> Metadata { get; }

    }
}