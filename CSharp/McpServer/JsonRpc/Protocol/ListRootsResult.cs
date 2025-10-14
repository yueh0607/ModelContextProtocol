using System.Collections.Generic;
using Newtonsoft.Json;

namespace JsonRpc.Protocol
{
    /// <summary>
    /// 表示客户端对服务器端 <see cref="RequestMethods.RootsList"/> 请求的响应，
    /// 包含可用的根。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 当服务器发送 <see cref="RequestMethods.RootsList"/> 请求以发现客户端上可用的根时，将返回此结果。
    /// </para>
    /// <para>
    /// 详情请参阅 <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see>。
    /// </para>
    /// </remarks>
    public class ListRootsResult : Result
    {
        [JsonProperty("roots", Required = Required.Always)]
        public IReadOnlyList<Root> Roots { get; set; }
    }
}