using ModelContextProtocol.Json;

namespace ModelContextProtocol.Protocol
{
    /// <summary>为具有名称（标识符）和标题（显示名称）属性的元数据提供基本接口。</summary>
    public interface IBaseMetadata
    {
        /// <summary>
        /// 获取或设置此项目的唯一标识符。
        /// </summary>
        [JsonProperty("name")]
        string Name { get; set; }

        /// <summary>
        /// 获取或设置标题。
        /// </summary>
        /// <remarks>
        /// 此属性适用于 UI 和最终用户环境。它经过优化，易于阅读和理解，
        /// 即使不熟悉特定领域术语的用户也能轻松理解。
        /// 如果未提供，则可以使用 <see cref="Name"/> 进行显示（工具除外，如果存在 <see cref="ToolAnnotations.Title"/>，则应优先于使用 <see cref="Name"/>）。
        /// </remarks>
        [JsonProperty("title")]
        string Title { get; set; }
    }
}