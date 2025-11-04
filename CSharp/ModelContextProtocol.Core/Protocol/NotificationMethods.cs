namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 提供 MCP 协议中常用通知方法的名称常量。
    /// </summary>
    public static class NotificationMethods
    {
        /// <summary>
        /// 当可用工具列表发生变化时，服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// 此通知告知客户端可用工具集已修改。
        /// 更改可能包括工具的添加、删除或更新。收到此通知后，客户端可以通过调用相应的
        /// 方法来刷新其工具列表，以获取更新的工具列表。
        /// </remarks>
        public const string ToolListChangedNotification = "notifications/tools/list_changed";

        /// <summary>
        /// 当可用提示列表发生变化时，服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// 此通知告知客户端可用提示集合已修改。
        /// 更改可能包括添加、删除或更新提示。
        /// 收到此通知后，客户端可以通过调用相应的方法来刷新其提示列表，以获取更新后的提示列表。
        /// </remarks>
        public const string PromptListChangedNotification = "notifications/prompts/list_changed";

        /// <summary>
        /// 当可用资源列表发生变化时，服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// 此通知告知客户端可用资源集合已修改。
        /// 更改可能包括添加、删除或更新资源。
        /// 收到此通知后，客户端可以通过调用相应的方法来刷新其资源列表，以获取更新后的资源列表。
        /// </remarks>
        public const string ResourceListChangedNotification = "notifications/resources/list_changed";

        /// <summary>
        /// 资源更新时服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// 此通知用于通知客户端其订阅的特定资源的变更。
        /// 当资源更新时，服务器会将此通知发送给所有订阅该资源的客户端。
        /// </remarks>
        public const string ResourceUpdatedNotification = "notifications/resources/updated";

        /// <summary>
        /// 客户端在根目录更新时发送的通知名称。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此通知告知服务器客户端的“根目录”已更改。
        /// 根目录定义了服务器在文件系统中可以操作的边界，使它们能够了解它们可以访问哪些目录和文件。
        /// 服务器可以从支持客户端请求根目录列表，并在该列表更改时接收通知。
        /// </para>
        /// <para>
        /// 收到此通知后，服务器可以通过调用相应的方法来刷新其根目录信息，该方法从客户端获取更新后的根目录列表。
        /// </para>
        /// </remarks>
        public const string RootsListChangedNotification = "notifications/roots/list_changed";

        /// <summary>
        /// 生成日志消息时服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 服务器使用此通知向客户端发送日志消息。日志消息可以包含不同的严重级别，例如调试、信息、警告或错误，
        /// 以及一个可选的日志记录器名称，用于标识源组件。
        /// </para>
        /// <para>
        /// 客户端可以使用
        /// <see cref="RequestMethods.LoggingSetLevel"/> 请求来控制触发通知的最低日志记录级别。
        /// 如果客户端未设置级别，服务器可能会根据其自身的配置确定要发送的消息。
        /// </para>
        /// </remarks>
        public const string LoggingMessageNotification = "notifications/message";

        /// <summary>
        /// 初始化完成后，客户端向服务器发送的通知名称。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 客户端在接收并处理服务器对
        /// <see cref="RequestMethods.Initialize"/> 请求的响应后发送此通知。
        /// 它表示客户端已准备好开始正常运行， 并且初始化阶段已完成。
        /// </para>
        /// <para>
        /// 收到此通知后，服务器可以开始发送通知并处理来自客户端的后续请求。
        /// </para>
        /// </remarks>
        public const string InitializedNotification = "notifications/initialized";

        /// <summary>
        /// 发送通知的名称，用于通知接收者长时间运行请求的进度更新。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此通知提供长时间运行操作的进度更新。
        /// 它包含一个将通知与特定请求关联的进度令牌、当前进度值、以及可选的总进度值和描述性消息。
        /// </para>
        /// <para>
        /// 进度通知可以由客户端或服务器发送，具体取决于上下文。
        /// 进度通知使客户端能够显示可能需要大量时间才能完成的操作的进度指示器，例如大文件上传、复杂计算或资源密集型处理任务。
        /// </para>
        /// </remarks>
        public const string ProgressNotification = "notifications/progress";

        /// <summary>
        /// 发送的通知名称，用于指示应取消先前发出的请求。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 从发出者的角度来看，请求应该仍在进行中。
        /// 但是，由于通信延迟，此通知始终有可能在请求完成后到达。
        /// </para>
        /// <para>
        /// 此通知指示结果将不会被使用，因此任何相关处理都应该停止。
        /// </para>
        /// <para>
        /// 客户端不得尝试取消其“初始化”请求。
        /// </para>
        /// </remarks>
        public const string CancelledNotification = "notifications/cancelled";
    }
}