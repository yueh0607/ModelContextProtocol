using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;

namespace ModelContextProtocol.Client
{
    public class McpClientHandlers
    {
        public IEnumerable<KeyValuePair<string, Func<JsonRpcNotification, CancellationToken, Task>>> NotificationHandlers { get; set; }

        public Func<ListRootsRequestParams, CancellationToken, Task<ListRootsResult>> RootsHandler { get; set; }


        public Func<ElicitRequestParams, CancellationToken, Task<ElicitResult>> ElicitationHandler { get; set; }


        public Func<CreateMessageRequestParams, IProgress<ProgressNotificationValue>, CancellationToken, Task<CreateMessageResult>> SamplingHandler { get; set; }
    }
}