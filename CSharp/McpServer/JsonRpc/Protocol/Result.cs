using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 为结果负载提供基类。
    /// </summary>
    public abstract class Result
    {
        /// <summary>防止外部派生</summary>
        protected Result()
        {
        }
        
        /// <summary>
        /// 元数据保留字段（协议级别保留的元信息）。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容做出假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }
    }
}