namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 定义 JSON-RPC 2.0 规范中规定的标准错误代码。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 这些错误代码遵循 JSON-RPC 2.0 规范。标准错误代码的范围为 -32768 到 -32000。
    /// -32000 到 -32099 的范围保留用于实现定义的服务器错误。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://www.jsonrpc.org/specification#error_object">JSON-RPC 2.0 规范</see>。
    /// </para>
    /// </remarks>
    public static class JsonRpcErrorCodes
    {
        /// <summary>
        /// 解析错误（Parse error）。
        /// </summary>
        /// <remarks>
        /// 无效的 JSON 被服务器接收。解析错误发生在接收到消息之前。
        /// 错误代码：-32700
        /// </remarks>
        public const int ParseError = -32700;

        /// <summary>
        /// 无效请求（Invalid Request）。
        /// </summary>
        /// <remarks>
        /// 接收到的 JSON 不是有效的请求对象。
        /// 错误代码：-32600
        /// </remarks>
        public const int InvalidRequest = -32600;

        /// <summary>
        /// 方法未找到（Method not found）。
        /// </summary>
        /// <remarks>
        /// 该方法不存在或不可用。
        /// 错误代码：-32601
        /// </remarks>
        public const int MethodNotFound = -32601;

        /// <summary>
        /// 无效参数（Invalid params）。
        /// </summary>
        /// <remarks>
        /// 无效的方法参数。
        /// 错误代码：-32602
        /// </remarks>
        public const int InvalidParams = -32602;

        /// <summary>
        /// 内部错误（Internal error）。
        /// </summary>
        /// <remarks>
        /// JSON-RPC 请求的内部处理错误。
        /// 错误代码：-32603
        /// </remarks>
        public const int InternalError = -32603;

        /// <summary>
        /// 服务器错误范围开始（Server error range start）。
        /// </summary>
        /// <remarks>
        /// 此范围内的错误代码（-32000 到 -32099）保留用于实现定义的服务器错误。
        /// 错误代码：-32000
        /// </remarks>
        public const int ServerErrorStart = -32000;

        /// <summary>
        /// 服务器错误范围结束（Server error range end）。
        /// </summary>
        /// <remarks>
        /// 错误代码：-32099
        /// </remarks>
        public const int ServerErrorEnd = -32099;
    }
}

