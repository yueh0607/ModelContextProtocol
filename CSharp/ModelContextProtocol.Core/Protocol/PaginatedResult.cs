namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 为支持基于游标分页的结果负载提供一个基类。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当可能返回大量结果或动态计算的结果可能产生可测量的延迟时，
    /// 分页功能可以将 API 响应分解为更小、更易于管理的块。
    /// </para>
    /// <para>
    /// 继承自 <see cref="PaginatedResult"/> 的类实现基于游标的分页，
    /// 其中 <see cref="NextCursor"/> 属性用作指向下一组结果的不透明令牌。
    /// </para>
    /// </remarks>
    public abstract class PaginatedResult : Result
    {
        private protected PaginatedResult()
        {
        }

        /// <summary>
        /// 获取或设置一个不透明的令牌，表示上次返回结果后的分页位置。
        /// </summary>
        /// <remarks>
        /// 当分页结果中有更多可用数据时，<see cref="NextCursor"/>
        /// 属性将包含一个非 <see langword="null"/> 令牌，可用于后续请求获取下一页。
        /// 当没有更多结果可返回时，<see cref="NextCursor"/> 属性将为 <see langword="null"/>。
        /// </remarks>
        public string NextCursor { get; set; }
    }
}