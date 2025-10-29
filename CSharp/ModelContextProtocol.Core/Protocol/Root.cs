using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 表示模型上下文协议中的根 URI 及其元数据。
    /// </summary>
    /// <remarks>
    /// 根 URI 用作资源导航的入口点，通常表示可访问和遍历的顶级目录或容器资源。
    /// 根 URI 提供了一个用于组织和访问协议内资源的层次结构。
    /// 每个根 URI 都有一个唯一标识它的 URI 以及可选的元数据，例如人类可读的名称。
    /// </remarks>
    public sealed class Root
    {
        /// <summary>
        /// 获取或设置根的 URI。
        /// </summary>
        [JsonProperty("uri", Required = Required.Always)]
        public string Uri { get; set; }

        /// <summary>
        /// 获取或设置根目录的可读名称。
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 获取或设置根目录的附加元数据。
        /// </summary>
        /// <remarks>
        /// 协议保留此字段以供将来使用。
        /// </remarks>
        [JsonProperty("_meta")]
        public JToken Meta { get; set; }
    }
}