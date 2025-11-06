using System;
using System.Collections.Generic;

using ModelContextProtocol.Json;
namespace ModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示可附加到内容、资源和资源模板的注解。
    /// </summary>
    /// <remarks>
    /// 注解支持针对不同受众筛选和设置内容优先级。
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </remarks>
    public sealed class Annotations
    {
        /// <summary>
        /// 获取或设置此内容的目标受众，以 <see cref="Role"/> 值的数组形式表示。
        /// </summary>
        [JsonProperty("audience")]
        public IList<Role> Audience { get; set; }

        /// <summary>
        /// 获取或设置一个值，指示此数据对于服务器运行的重要性。
        /// </summary>
        /// <remarks>
        /// 该值是介于 0 和 1 之间的浮点数，
        /// 其中 0 表示最低优先级 1 表示最高优先级。
        /// </remarks>
        [JsonProperty("priority")]
        public float? Priority { get; set; }

        /// <summary>
        /// 获取或设置资源最后修改的时间。
        /// </summary>
        /// <remarks>
        /// 对应的 JSON 应为 ISO 8601 格式的字符串（例如，“2025-01-12T15:00:58Z”）。
        /// 示例：打开文件中的最后活动时间戳、资源附加时的时间戳等。
        /// </remarks>
        [JsonProperty("lastModified")]
        public DateTimeOffset? LastModified { get; set; }
    }
}