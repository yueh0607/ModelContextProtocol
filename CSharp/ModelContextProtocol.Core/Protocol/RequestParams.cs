using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MapleModelContextProtocol.Protocol
{
    /// <summary>
    /// 为所有 request parameters 提供基类。
    /// </summary>
    /// <remarks>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">通信结构定义</see>。
    /// </remarks>
    public abstract class RequestParams
    {
     
        protected RequestParams()
        {
        }

        /// <summary>
        /// 协议级保留的元数据字段 (metadata)，以 JSON 对象表示。
        /// </summary>
        /// <remarks>
        /// 实现不得对其内容做出假设。
        /// </remarks>
        [JsonProperty("_meta")]
        public JObject Meta { get; set; }

        /// <summary>
        /// 获取或设置一个不透明令牌，该令牌将附加到任何后续进度通知中。
        /// </summary>
        [JsonIgnore]
        public ProgressToken? ProgressToken
        {
            get
            {
                if (Meta != null && Meta.TryGetValue("progressToken", out JToken token))
                {
                    if (token.Type == JTokenType.String)
                    {
                        return new ProgressToken(token.Value<string>());
                    }
                    else if (token.Type == JTokenType.Integer)
                    {
                        return new ProgressToken(token.Value<long>());
                    }
                }

                return null;
            }
            set
            {
                if (value is null)
                {
                    Meta?.Remove("progressToken");
                }
                else
                {
                    if (Meta == null)
                        Meta = new JObject();

                    object tok = value.Value.Token;
                    switch (tok)
                    {
                        case string str:
                            Meta["progressToken"] = JValue.CreateString(str);
                            break;
                        case long longValue:
                            Meta["progressToken"] = new JValue(longValue);
                            break;
                        default:
                            throw new InvalidOperationException("ProgressToken must be a string or a long.");
                    }
                }
            }
        }
    }
}