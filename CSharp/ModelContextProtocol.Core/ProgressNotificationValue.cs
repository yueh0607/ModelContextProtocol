namespace ModelContextProtocol
{
    /// <summary>提供可使用 <see cref="IProgress{ProgressNotificationValue}"/> 发送的进度值。</summary>
    public class ProgressNotificationValue
    {
        /// <summary>
        /// 获取或设置目前的进度。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 此值通常表示百分比 (0-100) 或迄今为止已处理的项目数（与 <see cref="Total"/> 属性一起使用时）。
        /// </para>
        /// <para>
        /// 报告进度时，此值应随着操作的进行单调递增。
        /// 表示百分比时，值通常在 0 到 100 之间，或者可以是任何正数与 <see cref="Total"/> 属性结合表示已完成的项目。
        /// </para>
        /// </remarks>
        public float Progress { get; set; } // TODO: 低版本语言不支持 required 关键字，这个属性一定要有值

        /// <summary>获取或设置要处理的项目总数（或所需的总进度）（如果已知）。</summary>
        public float? Total { get; set; }

        /// <summary>获取或设置描述当前进度的可选消息。</summary>
        public string Message { get; set; }
    }
}