using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapleModelContextProtocol.Protocol;

namespace MapleModelContextProtocol.Client
{
    public class McpClientHandlers
    {
        public IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, ValueTask>>> NotificationHandlers { get; set; }

        public Func<ListRootsRequestParams, CancellationToken, ValueTask<ListRootsResult>> RootsHandler { get; set; }


        public Func<ElicitRequestParams, CancellationToken, ValueTask<ElicitResult>> ElicitationHandler { get; set; }


        public Func<CreateMessageRequestParams, IProgress<ProgressNotificationValue>, CancellationToken, ValueTask<CreateMessageResult>> SamplingHandler { get; set; }
    }
}